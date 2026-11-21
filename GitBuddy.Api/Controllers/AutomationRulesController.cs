using GitBuddy.Api.Services.Automation;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Controllers;

[ApiController]
[Route("api/admin/automation-rules")]
[Authorize(Roles = "Admin")]
public class AutomationRulesController(AppDbContext context) : BaseController(context)
{
    private readonly AppDbContext _db = context;

    [HttpGet]
    public async Task<ActionResult<List<AutomationRuleDto>>> List([FromQuery] string? repository)
    {
        var query = _db.AutomationRules.Include(r => r.CreatedByUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(repository))
            query = query.Where(r => r.RepositoryFullName == repository);

        var rules = await query.OrderBy(r => r.Name).ToListAsync();
        return Ok(rules.Select(AutomationRuleDto.From).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AutomationRuleDto>> Get(int id)
    {
        var rule = await _db.AutomationRules.Include(r => r.CreatedByUser).FirstOrDefaultAsync(r => r.Id == id);
        return rule == null ? NotFound() : Ok(AutomationRuleDto.From(rule));
    }

    [HttpPost]
    public async Task<ActionResult<AutomationRuleDto>> Create([FromBody] UpsertAutomationRuleRequest request)
    {
        var user = await CurrentUser();
        if (user == null) return Unauthorized();

        var errors = Validate(request);
        if (errors.Count > 0) return BadRequest(new { errors });

        var now = DateTime.UtcNow;
        var rule = new AutomationRule { CreatedBy = user.Id, CreatedAt = now };
        Apply(rule, request, now);
        _db.AutomationRules.Add(rule);
        await _db.SaveChangesAsync();

        rule.CreatedByUser = user;
        return CreatedAtAction(nameof(Get), new { id = rule.Id }, AutomationRuleDto.From(rule));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AutomationRuleDto>> Update(int id, [FromBody] UpsertAutomationRuleRequest request)
    {
        var rule = await _db.AutomationRules.Include(r => r.CreatedByUser).FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null) return NotFound();

        var errors = Validate(request);
        if (errors.Count > 0) return BadRequest(new { errors });

        // Actions run with the creator's GitHub token, so whoever edits a rule becomes its new owner.
        var user = await CurrentUser();
        if (user != null && user.Id != rule.CreatedBy)
        {
            rule.CreatedBy = user.Id;
            rule.CreatedByUser = user;
        }

        Apply(rule, request, DateTime.UtcNow);
        await _db.SaveChangesAsync();
        return Ok(AutomationRuleDto.From(rule));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.AutomationRules.FindAsync(id);
        if (rule == null) return NotFound();

        _db.AutomationRules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/enabled")]
    public async Task<ActionResult<AutomationRuleDto>> SetEnabled(int id, [FromBody] SetEnabledRequest request)
    {
        var rule = await _db.AutomationRules.Include(r => r.CreatedByUser).FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null) return NotFound();

        rule.Enabled = request.Enabled;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(AutomationRuleDto.From(rule));
    }

    [HttpGet("{id:int}/runs")]
    public async Task<ActionResult<List<AutomationRunLogDto>>> Runs(int id, [FromQuery] int take = 50, [FromQuery] int skip = 0)
    {
        if (!await _db.AutomationRules.AnyAsync(r => r.Id == id)) return NotFound();

        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(skip, 0);

        var logs = await _db.AutomationRunLogs
            .Where(l => l.AutomationRuleId == id)
            .OrderByDescending(l => l.TriggeredAt)
            .Skip(skip).Take(take)
            .Join(_db.PullRequests, l => l.PullRequestId, p => p.Id,
                (l, p) => new { Log = l, p.Repository, p.GitHubId, p.Title })
            .ToListAsync();

        return Ok(logs.Select(x => new AutomationRunLogDto(
            x.Log.Id, x.Log.PullRequestId, x.Repository, x.GitHubId, x.Title, x.Log.TriggeredAt,
            x.Log.ConditionsMatched, x.Log.DryRun,
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(x.Log.ActionsTaken) ?? [],
            x.Log.Error)).ToList());
    }

    [HttpGet("templates")]
    public ActionResult<List<AutomationRuleTemplateDto>> Templates() =>
        Ok(AutomationCatalog.Templates.Select(t => new AutomationRuleTemplateDto(
            t.Key, t.Name, t.Description, t.TriggerType.ToString(), t.Conditions, t.Actions, t.MinIntervalSeconds)).ToList());

    [HttpGet("catalog")]
    public ActionResult<AutomationCatalogDto> Catalog() =>
        Ok(new AutomationCatalogDto(
            Enum.GetNames<AutomationTriggerType>().ToList(),
            AutomationCatalog.ConditionsByTrigger.ToDictionary(k => k.Key.ToString(), v => v.Value.ToList()),
            ActionTypes.All.ToList()));

    private static List<string> Validate(UpsertAutomationRuleRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required.");
        if (request.MinIntervalSeconds < 0) errors.Add("MinIntervalSeconds cannot be negative.");
        if (!Enum.TryParse<AutomationTriggerType>(request.TriggerType, out var trigger))
        {
            errors.Add($"Unknown trigger type '{request.TriggerType}'.");
            return errors;
        }

        errors.AddRange(AutomationCatalog.Validate(trigger, request.Conditions ?? [], request.Actions ?? []));
        return errors;
    }

    private static void Apply(AutomationRule rule, UpsertAutomationRuleRequest request, DateTime now)
    {
        rule.Name = request.Name.Trim();
        rule.RepositoryFullName = string.IsNullOrWhiteSpace(request.RepositoryFullName) ? null : request.RepositoryFullName.Trim();
        rule.Enabled = request.Enabled;
        rule.DryRun = request.DryRun;
        rule.TriggerType = Enum.Parse<AutomationTriggerType>(request.TriggerType);
        rule.MinIntervalSeconds = request.MinIntervalSeconds;
        rule.Conditions = AutomationJson.Serialize(request.Conditions ?? []);
        rule.Actions = AutomationJson.Serialize(request.Actions ?? []);
        rule.UpdatedAt = now;
    }
}

public record UpsertAutomationRuleRequest(
    string? RepositoryFullName,
    string Name,
    bool Enabled,
    bool DryRun,
    string TriggerType,
    int MinIntervalSeconds,
    List<ConditionDef>? Conditions,
    List<ActionDef>? Actions);

public record SetEnabledRequest(bool Enabled);

public record AutomationRuleDto(
    int Id,
    string? RepositoryFullName,
    string Name,
    bool Enabled,
    bool DryRun,
    string TriggerType,
    int MinIntervalSeconds,
    List<ConditionDef> Conditions,
    List<ActionDef> Actions,
    int CreatedBy,
    string? CreatedByUsername,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastTriggeredAt,
    int TriggerCount)
{
    public static AutomationRuleDto From(AutomationRule r) => new(
        r.Id, r.RepositoryFullName, r.Name, r.Enabled, r.DryRun, r.TriggerType.ToString(), r.MinIntervalSeconds,
        AutomationJson.ParseConditions(r.Conditions), AutomationJson.ParseActions(r.Actions),
        r.CreatedBy, r.CreatedByUser?.Username, r.CreatedAt, r.UpdatedAt, r.LastTriggeredAt, r.TriggerCount);
}

public record AutomationRunLogDto(
    int Id,
    int PullRequestId,
    string Repository,
    long PullRequestNumber,
    string PullRequestTitle,
    DateTime TriggeredAt,
    bool ConditionsMatched,
    bool DryRun,
    List<string> ActionsTaken,
    string? Error);

public record AutomationRuleTemplateDto(
    string Key,
    string Name,
    string Description,
    string TriggerType,
    List<ConditionDef> Conditions,
    List<ActionDef> Actions,
    int MinIntervalSeconds);

public record AutomationCatalogDto(
    List<string> Triggers,
    Dictionary<string, List<string>> ConditionsByTrigger,
    List<string> Actions);
