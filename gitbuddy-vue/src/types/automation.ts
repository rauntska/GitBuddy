export interface AutomationCondition {
  type: string;
  value?: string | null;
}

export interface AutomationAction {
  type: string;
  params?: Record<string, string> | null;
}

export interface AutomationRule {
  id: number;
  repositories: string[];
  name: string;
  enabled: boolean;
  dryRun: boolean;
  triggerType: string;
  minIntervalSeconds: number;
  conditions: AutomationCondition[];
  actions: AutomationAction[];
  createdBy: number;
  createdByUsername: string | null;
  createdAt: string;
  updatedAt: string;
  lastTriggeredAt: string | null;
  triggerCount: number;
}

export interface UpsertAutomationRuleRequest {
  repositories: string[];
  name: string;
  enabled: boolean;
  dryRun: boolean;
  triggerType: string;
  minIntervalSeconds: number;
  conditions: AutomationCondition[];
  actions: AutomationAction[];
}

export interface AutomationRunLog {
  id: number;
  pullRequestId: number;
  repository: string;
  pullRequestNumber: number;
  pullRequestTitle: string;
  triggeredAt: string;
  conditionsMatched: boolean;
  dryRun: boolean;
  actionsTaken: string[];
  error: string | null;
}

export interface AutomationRuleTemplate {
  key: string;
  name: string;
  description: string;
  triggerType: string;
  conditions: AutomationCondition[];
  actions: AutomationAction[];
  minIntervalSeconds: number;
}

export interface AutomationCatalog {
  triggers: string[];
  conditionsByTrigger: Record<string, string[]>;
  actions: string[];
}
