<template>
  <div class="border border-slate-700/30 rounded-xl bg-gradient-to-b from-slate-900/80 to-slate-950/80 shadow-lg mb-4">
    <!-- File Header -->
    <button
      class="sticky top-[8.5rem] z-10 flex items-center justify-between px-4 py-2 bg-gradient-to-r from-slate-800/60 to-slate-800/40 border-b border-slate-700/30 cursor-pointer hover:from-slate-800/80 hover:to-slate-800/60 select-none w-full text-left transition-all duration-200 backdrop-blur-sm"
      type="button"
      @click="onHeaderClick"
    >
      <div class="flex items-center gap-3 min-w-0">
        <!-- Expand/Collapse Icon -->
        <svg
          :class="['w-4 h-4 text-blue-400 transition-transform flex-shrink-0', { 'rotate-90': expanded }]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7" />
        </svg>

        <!-- File Status Badge -->
        <span :class="['text-xs font-bold px-2 py-0.5 rounded flex-shrink-0', getStatusBadgeClass(file.status || 'modified')]">
          {{ getStatusIcon(file.status || 'modified') }}
        </span>

        <!-- File Path -->
        <span class="text-sm font-mono text-slate-100 truncate tracking-wide">{{ file.path }}</span>
      </div>

      <!-- File Stats -->
      <div class="flex items-center gap-4 text-xs flex-shrink-0">
        <div class="flex items-center gap-2">
          <span class="text-emerald-400 font-semibold text-xs px-2 py-0.5 rounded bg-emerald-500/10 border border-emerald-500/20">+{{ file.additions }}</span>
          <span class="text-rose-400 font-semibold text-xs px-2 py-0.5 rounded bg-rose-500/10 border border-rose-500/20">-{{ file.deletions }}</span>
        </div>
        <div class="flex items-center gap-2 pl-3 border-l border-slate-600/50">
          <label class="flex items-center gap-2 cursor-pointer group">
            <input
              type="checkbox"
              :checked="file.viewedState === 'VIEWED'"
              @click.stop="toggleViewed"
              class="w-4 h-4 rounded border-slate-600 bg-slate-800 text-emerald-500 focus:ring-emerald-500/50 focus:ring-offset-slate-900 cursor-pointer transition-all"
              title="Mark as viewed"
            />
            <span class="text-xs text-slate-400 group-hover:text-slate-300 transition-colors">Viewed</span>
          </label>
        </div>
      </div>
    </button>

    <!-- File Diff Content -->
    <div v-if="expanded && !loading" class="relative bg-slate-950/50 backdrop-blur">
      <div v-if="hunks.length === 0" class="p-8 text-center">
        <div class="flex flex-col items-center gap-3">
          <svg class="w-12 h-12 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="text-slate-400 text-sm">No diff content available</p>
        </div>
      </div>

      <template v-if="hunks.length > 0">
        <!-- Minimap -->
        <div
          class="absolute right-0 top-0 bottom-0 w-1.5 bg-slate-800/40 border-l border-slate-700/30 z-20 rounded-l overflow-hidden"
        >
          <div
            v-for="(comment, index) in fileComments"
            :key="index"
            :style="{ top: getCommentPosition(comment.line) + '%' }"
            class="absolute w-full h-1.5 bg-gradient-to-b from-blue-500/80 to-blue-600/80 cursor-pointer hover:from-blue-400 hover:to-blue-500 transition-all shadow-sm"
            :title="`Comment by ${comment.author}`"
            @click="scrollToLine(comment.line)"
          />
        </div>

        <!-- Split View -->
        <div v-if="viewMode === 'split'" class="overflow-x-auto text-xs font-mono" ref="diffContainer">
          <table class="w-full border-collapse table-fixed">
            <colgroup>
              <col style="width: 56px;">
              <col style="width: calc(50% - 85px);">
              <col style="width: 56px;">
              <col style="width: 32px;">
              <col style="width: calc(50% - 59px);">
            </colgroup>
            <tbody>
              <template v-for="(hunk, hunkIndex) in hunks" :key="hunkIndex">
                <!-- Hunk Header -->
                <tr class="bg-gradient-to-r from-slate-800/40 to-slate-800/30 border-y border-slate-700/30">
                  <td colspan="5" class="px-4 py-2 text-slate-400 text-xs font-mono">
                    <span class="text-slate-500">@</span>@ -{{ hunk.oldStart }},{{ hunk.oldLines }} <span class="text-blue-400">+</span>{{ hunk.newStart }},{{ hunk.newLines }} <span class="text-slate-500">@</span>@
                  </td>
                </tr>

                <template v-for="(line, lineIndex) in getVisibleLines(hunk.lines)" :key="`${hunkIndex}-${lineIndex}`">
                  <tr
                    :ref="el => setLineRef(line.newLineNumber || line.oldLineNumber, el)"
                    :class="[
                      'hover:bg-slate-800/40 group transition-colors duration-150',
                      {
                        'bg-emerald-500/5 border-l-2 border-emerald-500/50': line.type === 'add',
                        'bg-rose-500/5 border-l-2 border-rose-500/50': line.type === 'delete',
                        'bg-amber-500/10 border-l-2 border-amber-500/50': highlightedLine === line.newLineNumber || highlightedLine === line.oldLineNumber,
                        'border-l-2 border-transparent': line.type !== 'add' && line.type !== 'delete' && highlightedLine !== line.newLineNumber && highlightedLine !== line.oldLineNumber
                      }
                    ]"
                  >
                    <!-- Old Side -->
                    <td class="px-3 py-1 text-slate-600 text-right select-none border-r border-slate-800 bg-slate-950/50">
                      {{ line.type === 'delete' ? line.oldLineNumber : '' }}
                    </td>
                    <td
                      :class="[
                        'px-4 py-1 font-mono text-sm overflow-hidden',
                        line.type === 'delete' ? 'bg-rose-950/10' : 'bg-slate-950/20'
                      ]"
                    >
                      <span v-if="line.type === 'delete'" class="text-rose-400 select-none mr-1">-</span>
                      <span v-else class="opacity-0 select-none mr-1">·</span>
                      <code
                        v-if="line.type === 'delete'"
                        :class="line.type === 'delete' ? 'diff-line-deleted' : 'diff-line-default'"
                        style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;"
                        v-html="highlightSyntax(line.content)"
                      />
                    </td>

                    <!-- New Side -->
                    <td class="px-3 py-1 text-slate-600 text-right select-none border-x border-slate-800 bg-slate-950/50">
                      {{ line.type !== 'delete' ? line.newLineNumber : '' }}
                    </td>
                    <td class="px-1.5 bg-slate-950/50">
                      <button
                        v-if="line.newLineNumber && line.type !== 'delete'"
                        @click="startComment(line.newLineNumber!)"
                        class="opacity-0 group-hover:opacity-100 p-1 hover:bg-slate-700 rounded-md transition-all duration-200"
                        title="Add comment"
                      >
                        <svg class="w-3.5 h-3.5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                        </svg>
                      </button>
                    </td>
                    <td
                      :class="[
                        'px-4 py-1 font-mono text-sm overflow-hidden',
                        line.type === 'add' ? 'bg-emerald-950/10' : 'bg-slate-950/30'
                      ]"
                    >
                      <span v-if="line.type === 'add'" class="text-emerald-400 select-none mr-1">+</span>
                      <span v-else class="opacity-0 select-none mr-1">·</span>
                      <code
                        v-if="line.type !== 'delete'"
                        :class="line.type === 'add' ? 'diff-line-added' : 'diff-line-default'"
                        style="white-space: pre-wrap; word-break: break-word; overflow-wrap: break-word;"
                        v-html="highlightSyntax(line.content)"
                      />
                    </td>
                  </tr>

                  <!-- Comments for Left Side (Old Code) -->
                  <tr v-if="line.type === 'delete' && (getCommentsForLine(line.oldLineNumber, 'left').length > 0 || (commentingLine === line.oldLineNumber && getCommentsForLine(line.oldLineNumber, 'left').length >= 0))">
                    <td colspan="2" class="p-0 bg-gradient-to-b from-slate-900/50 to-slate-950/30 border-t border-slate-700/20">
                      <div v-if="getCommentsForLine(line.oldLineNumber, 'left').length > 0" class="p-4 space-y-4">
                         <!-- Comment Threads -->
                        <div
                          v-for="[threadId, comments] in getCommentsGroupedByThread(line.oldLineNumber, 'left')"
                          :key="threadId || 'standalone-' + comments[0]?.id"
                          class="border border-slate-700/30 rounded-xl overflow-hidden"
                          :class="{ 'opacity-50': threadId && getThreadInfo(threadId)?.isResolved && !expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '') }"
                        >
                          <!-- Thread Header -->
                          <div
                            v-if="threadId"
                            class="px-4 py-3 bg-slate-800/60 border-b border-slate-700/30 flex items-center justify-between"
                          >
                            <div class="flex items-center gap-2">
                              <button
                                @click="toggleThreadExpanded(threadId)"
                                class="text-slate-400 hover:text-slate-200 transition-colors p-1 -ml-2"
                                title="Toggle thread"
                              >
                                <svg
                                  :class="['w-4 h-4 transition-transform', { 'rotate-90': !expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '') }]"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                                </svg>
                              </button>
                              <span class="text-xs font-medium text-slate-300">{{ comments.length }} comment{{ comments.length > 1 ? 's' : '' }}</span>
                            </div>
                            <div class="flex items-center gap-2">
                              <span
                                v-if="getThreadInfo(threadId)?.isOutdated"
                                class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
                              >
                                Outdated
                              </span>
                              <button
                                v-if="getThreadInfo(threadId)"
                                @click="handleResolveThread(threadId, !getThreadInfo(threadId)?.isResolved)"
                                :disabled="resolvingThreads.has(getThreadInfo(threadId)?.gitHubId || '')"
                                class="flex items-center gap-1 px-2 py-0.5 text-xs rounded-full border transition-all disabled:opacity-50"
                                :class="[
                                  getThreadInfo(threadId)?.isResolved
                                    ? 'bg-emerald-900/30 text-emerald-400 border-emerald-700/50'
                                    : 'bg-slate-700/30 text-slate-400 border-slate-600/50 hover:bg-slate-600/30'
                                ]"
                                :title="getThreadInfo(threadId)?.isResolved ? 'Mark as unresolved' : 'Mark as resolved'"
                              >
                                <svg v-if="getThreadInfo(threadId)?.isResolved" class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L14.586 9H4a1 1 0 110-2h10.586l-4.293 4.293z" clip-rule="evenodd" />
                                </svg>
                                <svg v-else class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                                </svg>
                              </button>
                            </div>
                          </div>


                           <!-- Comments in Thread -->
                           <div v-if="!threadId || expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '')" class="p-4 space-y-4">

                            <div
                              v-for="(comment, index) in comments"
                              :key="comment.id"
                              class="flex gap-3 relative"
                              :class="{ 'ml-8': index > 0 }"
                            >
                              <!-- Thread Connector Line -->
                              <div
                                v-if="index > 0"
                                class="absolute left-4 top-0 bottom-0 w-0.5 bg-slate-700/30"
                                style="margin-left: -2px;"
                              ></div>

                              <img
                                v-if="comment.authorAvatar"
                                :src="comment.authorAvatar"
                                :alt="comment.author"
                                class="w-8 h-8 rounded-full flex-shrink-0 ring-2 ring-slate-700/50 z-10 relative"
                                :class="{ 'w-6 h-6': index > 0 }"
                              />
                              <div class="flex-1 min-w-0">
                                <div class="flex items-center gap-2 mb-2">
                                  <span class="text-sm font-semibold text-slate-100">{{ comment.author }}</span>
                                  <span class="text-xs text-slate-500">{{ formatRelativeTime(comment.createdAt) }}</span>
                                  <!-- Outdated Badge -->
                                  <span
                                    v-if="comment.isOutdated && !threadId"
                                    class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
                                  >
                                    Outdated
                                  </span>
                                </div>

                                <!-- Comment Body -->
                                <p class="text-sm text-slate-300 leading-relaxed whitespace-pre-wrap">{{ comment.body }}</p>

                                <!-- Reply Form -->
                                <div v-if="replyingToCommentId === comment.id" class="mt-3">
                                  <textarea
                                    v-model="replyText"
                                    ref="replyTextarea"
                                    placeholder="Add your reply..."
                                    class="w-full px-4 py-3 bg-slate-900/80 border border-slate-600/50 rounded-xl text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all duration-200 placeholder:text-slate-500"
                                    rows="3"
                                  />
                                  <div v-if="replyErrors.get(comment.id)" class="mt-2 text-xs text-rose-400">{{ replyErrors.get(comment.id) }}</div>
                                  <div class="flex gap-2 justify-end mt-3">
                                    <button
                                      @click="cancelReply"
                                      class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
                                    >
                                      Cancel
                                    </button>
                                    <button
                                      @click="submitReply"
                                      :disabled="!replyText.trim()"
                                      class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
                                    >
                                      Reply
                                    </button>
                                  </div>
                                </div>

                                <!-- Reply Button (only on last comment) -->
                                <div
                                  v-if="isLastCommentInThread(threadId, comments.map(c => c.id)) && index === comments.length - 1 && replyingToCommentId !== comment.id"
                                  class="flex-shrink-0"
                                >
                                  <button
                                    @click="startReplyToComment(comment.id, threadId)"
                                    class="flex items-center gap-1 px-3 py-1 text-xs text-blue-400 hover:text-blue-300 hover:bg-blue-500/10 rounded-lg transition-all duration-200"
                                  >
                                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v10M3 10l6 6m-6-6v6" />
                                    </svg>
                                    Reply
                                  </button>
                                </div>

                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                      <!-- New Comment Form for Left Side -->
                      <div v-if="commentingLine === line.oldLineNumber" class="p-4 border-t border-slate-700/20">
                        <textarea
                          v-model="newCommentText"
                          ref="newCommentTextarea"
                          placeholder="Add your comment..."
                          class="w-full px-4 py-3 bg-slate-900/80 border border-slate-600/50 rounded-xl text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all duration-200 placeholder:text-slate-500"
                          rows="3"
                        />
                        <div v-if="commentError" class="mt-2 text-xs text-rose-400">{{ commentError }}</div>
                        <div class="flex gap-2 justify-end mt-3">
                          <button
                            @click="cancelNewComment"
                            class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
                          >
                            Cancel
                          </button>
                          <button
                            @click="submitNewComment"
                            :disabled="!newCommentText.trim()"
                            class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
                          >
                            Add Comment
                          </button>
                        </div>
                      </div>
                    </td>
                    <td colspan="3" class="p-0 bg-slate-950/30"></td>
                  </tr>

                  <!-- Comments for Right Side (New Code) -->
                  <tr v-if="line.type !== 'delete' && (getCommentsForLine(line.newLineNumber, 'right').length > 0 || (commentingLine === line.newLineNumber))">
                    <td colspan="3" class="p-0 bg-slate-950/30"></td>
                    <td colspan="2" class="p-0 bg-gradient-to-b from-slate-900/50 to-slate-950/30 border-t border-slate-700/20">
                      <div v-if="getCommentsForLine(line.newLineNumber, 'right').length > 0" class="p-4 space-y-4">
                         <!-- Comment Threads -->
                        <div
                          v-for="[threadId, comments] in getCommentsGroupedByThread(line.newLineNumber, 'right')"
                          :key="threadId || 'standalone-' + comments[0]?.id"
                          class="border border-slate-700/30 rounded-xl overflow-hidden"
                          :class="{ 'opacity-50': threadId && getThreadInfo(threadId)?.isResolved && !expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '') }"
                        >
                          <!-- Thread Header -->
                          <div
                            v-if="threadId"
                            class="px-4 py-3 bg-slate-800/60 border-b border-slate-700/30 flex items-center justify-between"
                          >
                            <div class="flex items-center gap-2">
                              <button
                                @click="toggleThreadExpanded(threadId)"
                                class="text-slate-400 hover:text-slate-200 transition-colors p-1 -ml-2"
                                title="Toggle thread"
                              >
                                <svg
                                  :class="['w-4 h-4 transition-transform', { 'rotate-90': !expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '') }]"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                                </svg>
                              </button>
                              <span class="text-xs font-medium text-slate-300">{{ comments.length }} comment{{ comments.length > 1 ? 's' : '' }}</span>
                            </div>
                            <div class="flex items-center gap-2">
                              <span
                                v-if="getThreadInfo(threadId)?.isOutdated"
                                class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
                              >
                                Outdated
                              </span>
                              <button
                                v-if="getThreadInfo(threadId)"
                                @click="handleResolveThread(threadId, !getThreadInfo(threadId)?.isResolved)"
                                :disabled="resolvingThreads.has(getThreadInfo(threadId)?.gitHubId || '')"
                                class="flex items-center gap-1 px-2 py-0.5 text-xs rounded-full border transition-all disabled:opacity-50"
                                :class="[
                                  getThreadInfo(threadId)?.isResolved
                                    ? 'bg-emerald-900/30 text-emerald-400 border-emerald-700/50'
                                    : 'bg-slate-700/30 text-slate-400 border-slate-600/50 hover:bg-slate-600/30'
                                ]"
                                :title="getThreadInfo(threadId)?.isResolved ? 'Mark as unresolved' : 'Mark as resolved'"
                              >
                                <svg v-if="getThreadInfo(threadId)?.isResolved" class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L14.586 9H4a1 1 0 110-2h10.586l-4.293 4.293z" clip-rule="evenodd" />
                                </svg>
                                <svg v-else class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                                </svg>
                              </button>
                            </div>
                          </div>

                           <!-- Comments in Thread -->
                           <div v-if="!threadId || expandedThreads.has(getThreadInfo(threadId)?.gitHubId || '')" class="p-4 space-y-4">

                            <div
                              v-for="(comment, index) in comments"
                              :key="comment.id"
                              class="flex gap-3 relative"
                              :class="{ 'ml-8': index > 0 }"
                            >
                              <!-- Thread Connector Line -->
                              <div
                                v-if="index > 0"
                                class="absolute left-4 top-0 bottom-0 w-0.5 bg-slate-700/30"
                                style="margin-left: -2px;"
                              ></div>

                              <img
                                v-if="comment.authorAvatar"
                                :src="comment.authorAvatar"
                                :alt="comment.author"
                                class="w-8 h-8 rounded-full flex-shrink-0 ring-2 ring-slate-700/50 z-10 relative"
                                :class="{ 'w-6 h-6': index > 0 }"
                              />
                              <div class="flex-1 min-w-0">
                                <div class="flex items-center gap-2 mb-2">
                                  <span class="text-sm font-semibold text-slate-100">{{ comment.author }}</span>
                                  <span class="text-xs text-slate-500">{{ formatRelativeTime(comment.createdAt) }}</span>
                                  <!-- Outdated Badge -->
                                  <span
                                    v-if="comment.isOutdated && !threadId"
                                    class="px-2 py-0.5 text-xs bg-amber-900/30 text-amber-400 rounded-full border border-amber-700/50"
                                  >
                                    Outdated
                                  </span>
                                </div>

                                <!-- Comment Body -->
                                <p class="text-sm text-slate-300 leading-relaxed whitespace-pre-wrap">{{ comment.body }}</p>

                                <!-- Reply Form -->
                                <div v-if="replyingToCommentId === comment.id" class="mt-3">
                                  <textarea
                                    v-model="replyText"
                                    ref="replyTextarea"
                                    placeholder="Add your reply..."
                                    class="w-full px-4 py-3 bg-slate-900/80 border border-slate-600/50 rounded-xl text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all duration-200 placeholder:text-slate-500"
                                    rows="3"
                                  />
                                  <div v-if="replyErrors.get(comment.id)" class="mt-2 text-xs text-rose-400">{{ replyErrors.get(comment.id) }}</div>
                                  <div class="flex gap-2 justify-end mt-3">
                                    <button
                                      @click="cancelReply"
                                      class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
                                    >
                                      Cancel
                                    </button>
                                    <button
                                      @click="submitReply"
                                      :disabled="!replyText.trim()"
                                      class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
                                    >
                                      Reply
                                    </button>
                                  </div>
                                </div>

                                <!-- Reply Button (only on last comment) -->
                                <div
                                  v-if="isLastCommentInThread(threadId, comments.map(c => c.id)) && index === comments.length - 1 && replyingToCommentId !== comment.id"
                                  class="flex-shrink-0"
                                >
                                  <button
                                    @click="startReplyToComment(comment.id, threadId)"
                                    class="flex items-center gap-1 px-3 py-1 text-xs text-blue-400 hover:text-blue-300 hover:bg-blue-500/10 rounded-lg transition-all duration-200"
                                  >
                                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v10M3 10l6 6m-6-6v6" />
                                    </svg>
                                    Reply
                                  </button>
                                </div>

                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                      <!-- New Comment Form for Right Side -->
                      <div v-if="commentingLine === line.newLineNumber" class="p-4 border-t border-slate-700/20">
                        <textarea
                          v-model="newCommentText"
                          ref="newCommentTextarea"
                          placeholder="Add your comment..."
                          class="w-full px-4 py-3 bg-slate-900/80 border border-slate-600/50 rounded-xl text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all duration-200 placeholder:text-slate-500"
                          rows="3"
                        />
                        <div v-if="commentError" class="mt-2 text-xs text-rose-400">{{ commentError }}</div>
                        <div class="flex gap-2 justify-end mt-3">
                          <button
                            @click="cancelNewComment"
                            class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-xs text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
                          >
                            Cancel
                          </button>
                          <button
                            @click="submitNewComment"
                            :disabled="!newCommentText.trim()"
                            class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
                          >
                            Add Comment
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                </template>
              </template>
            </tbody>
          </table>
        </div>
      </template>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="p-8 text-center bg-slate-950/30">
      <div class="flex flex-col items-center gap-3">
        <svg class="animate-spin h-8 w-8 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0-3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-sm text-slate-400">Loading diff...</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, watch } from 'vue';
import type { FileDiff, Comment, ReviewThread } from '../types';
import { parsePatch } from '../utils/diffHelpers';
import { highlightCode, detectLanguageFromPath } from '../utils/syntaxHighlight';
import { useUserPreferences } from '../composables/useUserPreferences';

const props = defineProps<{
  file: FileDiff;
  comments: Comment[];
  reviewThreads: ReviewThread[];
  onAddComment: (line: number, body: string) => Promise<void>;
  onReplyToThread?: (threadId: string, line: number, body: string) => Promise<void>;
  onResolveThread?: (threadId: string, resolved: boolean) => Promise<void>;
  initialExpanded?: boolean;
  viewed?: boolean;
  onToggleViewed?: (path: string, viewed: boolean) => void;
}>();

const emit = defineEmits<{
  toggleViewed: [path: string, viewed: boolean];
}>();

// Helper to convert thread database ID to GitHub Node ID (not currently used)
// const getThreadNodeId = (threadId: number | null): string | null => {
//   if (!threadId) return null;
//   const thread = fileReviewThreads.value.find(rt => rt.id === threadId);
//   return thread?.gitHubId ?? null;
// };

const { preferences } = useUserPreferences();

const expanded = ref(props.file.viewedState !== 'VIEWED' && !props.file.viewed);

watch(() => [props.file.viewedState, props.file.viewed] as const, ([newViewedState, newViewed]) => {
  expanded.value = newViewedState !== 'VIEWED' && newViewed !== true;
}, { deep: true, immediate: true });
 const loading = ref(false);
const hunks = ref<any[]>([]);
const commentingLine = ref<number | null>(null);
const lineRefs = ref<Map<number, HTMLElement>>(new Map());
const language = ref(props.file.path ? detectLanguageFromPath(props.file.path) : 'text');
const highlightedLine = ref<number | null>(null);
const replyingToCommentId = ref<number | null>(null);
const replyingToThread = ref<string | null>(null);
const replyText = ref('');
const replyTextarea = ref<HTMLTextAreaElement | null>(null);
const expandedThreads = ref<Set<string>>(new Set());
const replyErrors = ref<Map<number, string>>(new Map());
const resolvingThreads = ref<Set<string>>(new Set());
const newCommentText = ref('');
const commentError = ref('');
const newCommentTextarea = ref<HTMLTextAreaElement | null>(null);


const loadHunks = async () => {
  if (hunks.value.length === 0) {
    const patchData = (props.file as any).patch || (props.file as any).Patch || (props.file as any).diff;
    if (patchData) {
      loading.value = true;
      await new Promise(resolve => setTimeout(resolve, 100));
      hunks.value = parsePatch(patchData);
      loading.value = false;
    } else {
      loading.value = false;
    }
  }
};

onMounted(() => {
  if (expanded.value) {
    loadHunks();
  }
});

watch(expanded, (newValue) => {
  if (newValue) {
    loadHunks();
  }
});

const viewMode = computed(() => preferences.value.diffViewMode);

const fileComments = computed(() =>
  props.comments.filter(c => c.path === props.file.path && c.line)
);

const fileReviewThreads = computed(() =>
  props.reviewThreads.filter(rt => rt.path === props.file.path)
);

const onHeaderClick = () => {
  expanded.value = !expanded.value;
};

const toggleViewed = (event: Event) => {
  const target = event.target as HTMLInputElement;
  const newViewed = target.checked;
  if (props.file.path) {
    emit('toggleViewed', props.file.path, newViewed);
    if (props.onToggleViewed) {
      props.onToggleViewed(props.file.path, newViewed);
    }
  }
};

const getVisibleLines = (lines: any[]) => {
  if (preferences.value.showContext) return lines;

  const result: any[] = [];
  const contextLines = 5;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const isEmptyLine = !line.content || line.content.trim() === '';

    if (line.type !== 'context') {
      if (!isEmptyLine) {
        result.push(line);
      }
      continue;
    }

    let hasNearbyChange = false;
    for (let j = Math.max(0, i - contextLines); j <= Math.min(lines.length - 1, i + contextLines); j++) {
      if (lines[j].type !== 'context') {
        hasNearbyChange = true;
        break;
      }
    }

    if (hasNearbyChange && !isEmptyLine) {
      result.push(line);
    }
  }

  return result;
};

const highlightSyntax = (code: string): string => {
  return highlightCode(code, language.value);
};

const getStatusIcon = (status: string) => {
  const icons: Record<string, string> = {
    added: 'A',
    modified: 'M',
    deleted: 'D',
    renamed: 'R',
  };
  return icons[status] || 'M';
};

const getStatusBadgeClass = (status: string) => {
  const classes: Record<string, string> = {
    added: 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30',
    modified: 'bg-amber-500/10 text-amber-400 border border-amber-500/30',
    deleted: 'bg-rose-500/10 text-rose-400 border border-rose-500/30',
    renamed: 'bg-blue-500/10 text-blue-400 border border-blue-500/30',
  };
  return classes[status] || 'bg-slate-500/10 text-slate-400 border border-slate-500/30';
};

const startComment = async (line: number) => {
  commentingLine.value = line;
  newCommentText.value = '';
  commentError.value = '';
  await nextTick();
  newCommentTextarea.value?.focus();
};

const cancelNewComment = () => {
  commentingLine.value = null;
  newCommentText.value = '';
  commentError.value = '';
};

const submitNewComment = async () => {
  if (!newCommentText.value.trim() || commentingLine.value === null) return;

  try {
    await props.onAddComment(commentingLine.value, newCommentText.value);
    newCommentText.value = '';
    commentingLine.value = null;
    commentError.value = '';
  } catch (error) {
    commentError.value = 'Failed to add comment';
    console.error('Failed to add comment:', error);
  }
};



const getCommentsForLine = (line: number | undefined, side?: 'left' | 'right'): Comment[] => {
  if (!line) return [];

  const comments = fileComments.value.filter(c => c.line === line);

  if (!side) return comments;

  const expectedSide = side === 'left' ? 'LEFT' : 'RIGHT';

  return comments.filter(comment => {
    if (!comment.reviewThreadId) return true;

    const reviewThread = fileReviewThreads.value.find(rt => rt.id === comment.reviewThreadId);
    if (!reviewThread) {
      console.log('Comment has reviewThreadId but no matching review thread found:', {
        commentReviewThreadId: comment.reviewThreadId,
        allReviewThreadIds: fileReviewThreads.value.map(rt => rt.id)
      });
      return true;
    }

    const diffSideMatch = !reviewThread.diffSide || reviewThread.diffSide.toUpperCase() === expectedSide;

    if (!diffSideMatch) {
      console.log('Comment filtered by diffSide:', {
        commentId: comment.id,
        reviewThreadId: reviewThread.id,
        reviewThreadDiffSide: reviewThread.diffSide,
        expectedSide
      });
    }

    return diffSideMatch;
  });
};

const getCommentsGroupedByThread = (line: number | undefined, side?: 'left' | 'right') => {
  const comments = getCommentsForLine(line, side);

  const threadMap = new Map<number | null, Comment[]>();

  comments.forEach(comment => {
    const threadId = comment.reviewThreadId ?? null;
    if (!threadMap.has(threadId)) {
      threadMap.set(threadId, []);
    }
    threadMap.get(threadId)!.push(comment);
  });

  threadMap.forEach(comments => {
    comments.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  });

  return Array.from(threadMap.entries());
};

const getThreadInfo = (threadId: number | null) => {
  if (!threadId) return null;
  return fileReviewThreads.value.find(rt => rt.id === threadId);
};

const getCommentPosition = (line: number | undefined): number => {
  if (!line) return 0;
  const totalLines = hunks.value.reduce((sum, hunk) => sum + hunk.lines.length, 0);
  const lineIndex = hunks.value.reduce((sum, hunk) => {
    const lineInHunk = hunk.lines.findIndex((l: any) => l.newLineNumber === line);
    return lineInHunk >= 0 ? sum + lineInHunk : sum + hunk.lines.length;
  }, 0);
  return (lineIndex / totalLines) * 100;
};

const scrollToLine = (line: number | undefined) => {
  if (!line) return;
  const el = lineRefs.value.get(line);
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    highlightLine(line);
  }
};

const highlightLine = (lineNumber: number) => {
  highlightedLine.value = lineNumber;
  setTimeout(() => {
    highlightedLine.value = null;
  }, 2000);
};

const setLineRef = (lineNumber: number | undefined, el: any) => {
  if (lineNumber && el) {
    lineRefs.value.set(lineNumber, el);
  }
};

defineExpose({
  highlightLine,
  expanded,
});

  const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
};

const isLastCommentInThread = (threadId: number | null, commentIds: number[]): boolean => {
  if (!threadId) return true;
  const threadComments = fileComments.value.filter(c => c.reviewThreadId === threadId);
  if (threadComments.length === 0) return true;
  const lastComment = threadComments[threadComments.length - 1];
  return lastComment ? commentIds.includes(lastComment.id) : false;
};

const startReplyToComment = (commentId: number, threadId: number | null) => {
  replyingToCommentId.value = commentId;
  const thread = threadId ? getThreadInfo(threadId) : null;
  replyingToThread.value = thread?.gitHubId || null;
  replyText.value = '';
  nextTick(() => {
    replyTextarea.value?.focus();
  });
};

const cancelReply = () => {
  replyingToCommentId.value = null;
  replyingToThread.value = null;
  replyText.value = '';
  replyErrors.value.delete(replyingToCommentId.value ?? 0);
};

const submitReply = async () => {
  if (!replyText.value.trim() || !replyingToCommentId.value || !replyingToThread.value) return;

  try {
    if (props.onReplyToThread) {
      await props.onReplyToThread(replyingToThread.value, commentingLine.value ?? 0, replyText.value);
      replyText.value = '';
      replyingToCommentId.value = null;
      replyingToThread.value = null;
      replyErrors.value.delete(replyingToCommentId.value ?? 0);
    }
  } catch (error) {
    if (replyingToCommentId.value !== null) {
      replyErrors.value.set(replyingToCommentId.value, 'Failed to add reply');
    }
  }
};

  const handleResolveThread = async (threadId: number | null, resolved: boolean) => {
  if (!threadId || !props.onResolveThread) return;

  const threadInfo = getThreadInfo(threadId);
  if (!threadInfo) return;

  const threadNodeId = threadInfo.gitHubId;
  resolvingThreads.value.add(threadNodeId);

  try {
    await props.onResolveThread(threadNodeId, resolved);
  } catch (error) {
    console.error('Failed to resolve thread:', error);
  } finally {
    resolvingThreads.value.delete(threadNodeId);
  }
};

const toggleThreadExpanded = (threadId: number | null) => {
  if (!threadId) return;
  const threadInfo = getThreadInfo(threadId);
  if (!threadInfo) return;

  const threadNodeId = threadInfo.gitHubId;
  if (expandedThreads.value.has(threadNodeId)) {
    expandedThreads.value.delete(threadNodeId);
  } else {
    expandedThreads.value.add(threadNodeId);
  }
};


</script>

<style scoped>
@import url('../styles/github-dark-syntax.css');
</style>
