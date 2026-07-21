<template>
  <div class="flex flex-col bg-slate-900 text-slate-200 min-h-screen">
    <!-- Compact Header -->
    <div class="sticky top-20 z-20 bg-slate-900/95 border-b border-slate-800">
      <div class="px-3 sm:px-5 py-3 flex flex-wrap items-center gap-2 sm:gap-3">
        <Breadcrumb
          v-if="prDetail"
          :items="[
            { label: 'Dashboard', to: '/' },
            { label: prDetail.repository },
            { label: `#${prDetail.gitHubId}` },
          ]"
        />
        <div v-else class="text-slate-500 text-sm font-mono">Loading...</div>

        <div class="flex-1 min-w-0 flex items-center gap-2 sm:gap-3 overflow-hidden">
          <StatusBadge v-if="prDetail" :status="prDetail.status" />
          
          <div
            v-if="prDetail && !prDetail.isMerged && !prDetail.draft && prDetail.status !== 'Merged' && prDetail.status !== 'Closed'"
            :class="[
              'px-2 sm:px-3 py-1 rounded-full text-xs border',
              prDetail.isMergeReady
                ? 'bg-emerald-950/30 text-emerald-400 border-emerald-900/50'
                : 'bg-amber-950/20 text-amber-400 border-amber-900/50'
            ]"
            :title="prDetail.mergeBlockReason || 'Ready to merge'"
          >
            <span v-if="prDetail.isMergeReady" class="flex items-center gap-1 sm:gap-1.5">
              <span class="font-mono leading-none text-emerald-400">✓</span>
              <span class="hidden sm:inline">Ready to merge</span>
            </span>
            <span v-else-if="prDetail.requiredApprovingReviews" class="flex items-center gap-1 sm:gap-1.5 font-mono tabular-nums">
              <span class="leading-none text-amber-400">⋯</span>
              <span class="hidden sm:inline">{{ prDetail.currentApprovingReviews }}/{{ prDetail.requiredApprovingReviews }} approvals</span>
              <span class="sm:hidden">{{ prDetail.currentApprovingReviews }}/{{ prDetail.requiredApprovingReviews }}</span>
              <span v-if="prDetail.hasUnresolvedThreads" class="ml-1 hidden sm:inline">• {{ prDetail.reviewThreads.filter(rt => !rt.isResolved && !rt.isOutdated).length }} unresolved</span>
            </span>
            <span v-else class="flex items-center gap-1 sm:gap-1.5">
              <span class="hidden sm:inline">{{ prDetail.mergeBlockReason || 'Review required' }}</span>
            </span>
          </div>

          <div class="flex-1 min-w-0 flex items-center gap-2 group/title">
            <template v-if="isEditingTitle">
              <input
                ref="titleInputRef"
                v-model="editTitleValue"
                @keydown="handleTitleKeydown"
                @blur="savingTitle ? null : saveTitle()"
                :disabled="savingTitle"
                class="flex-1 min-w-0 bg-slate-800 border border-slate-600 rounded px-2 py-1 text-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-500/50"
                :class="{ 'opacity-50': savingTitle }"
              />
              <svg v-if="savingTitle" class="animate-spin h-4 w-4 text-slate-400 flex-shrink-0" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </template>
            <template v-else>
              <h1 class="text-lg text-slate-100 truncate tracking-tight min-w-0 flex-shrink">
                {{ prDetail?.title || 'Loading...' }}
              </h1>
              <button
                v-if="prDetail && !prDetail.isMerged"
                @click="startEditingTitle"
                class="p-1 rounded opacity-0 group-hover/title:opacity-100 hover:bg-slate-800 text-slate-400 hover:text-slate-200 transition-all flex-shrink-0"
                title="Edit title"
              >
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
            </template>
          </div>
        </div>

<div class="flex flex-wrap items-center gap-2 sm:gap-3 text-xs flex-shrink-0">
          <span class="text-slate-400 hidden sm:inline font-mono tabular-nums">{{ prDetail?.repository }} #{{ prDetail?.gitHubId }}</span>

          <button
            v-if="!prDetail?.isMerged"
            @click="showReviewModal = true"
            :class="[
              'px-3 sm:px-4 py-2 rounded-lg text-xs transition-all duration-150 relative',
              prDetail?.pendingReview?.comments?.length
                ? 'bg-slate-200 text-slate-900 hover:bg-white'
                : 'border border-slate-800 text-slate-300 hover:bg-slate-800'
            ]"
          >
            <span class="sm:hidden">Review</span>
            <span class="hidden sm:inline">{{ prDetail?.pendingReview?.comments?.length ? 'Finish Review' : 'Review' }}</span>
            <span
              v-if="prDetail?.pendingReview?.comments?.length"
              class="absolute -top-1.5 -right-1.5 bg-amber-400 text-slate-900 text-[10px] rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-0.5 font-mono tabular-nums"
            >
              {{ prDetail.pendingReview.comments.length }}
            </span>
          </button>

          <button
            v-if="prDetail?.draft && !prDetail?.isMerged"
            @click="handlePublishDraft"
            :disabled="publishingDraft"
            class="px-3 sm:px-4 py-2 bg-slate-200 hover:bg-white rounded-lg text-xs text-slate-900 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span class="sm:hidden">Pub</span>
            <span class="hidden sm:inline">{{ publishingDraft ? 'Publishing...' : 'Publish' }}</span>
          </button>

          <div v-if="prDetail && !prDetail.draft && !prDetail.isMerged" class="relative">
            <div class="flex items-stretch gap-0">
              <button
                @click="canMerge && handleMerge()"
                :disabled="!canMerge || merging"
                :class="[
                  'px-3 sm:px-4 py-2 rounded-l-lg text-xs transition-all duration-150',
                  canMerge && !merging
                    ? 'bg-slate-200 text-slate-900 hover:bg-white'
                    : 'bg-slate-800 text-slate-500 cursor-not-allowed',
                ]"
              >
                <span v-if="merging">Merging...</span>
                <span v-else-if="prDetail.isMerged">Merged</span>
                <span v-else>Merge</span>
              </button>
              <button
                @click="showMergeDropdown = !showMergeDropdown; loadMergeOptions()"
                :disabled="!canMerge || merging"
                :class="[
                  'px-2 py-2 rounded-r-lg text-xs border-l transition-all duration-150',
                  canMerge && !merging
                    ? 'bg-slate-200 text-slate-900 hover:bg-white border-slate-300'
                    : 'bg-slate-800 text-slate-500 cursor-not-allowed border-slate-700',
                ]"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>

            <!-- Merge Dropdown -->
            <div
              v-if="showMergeDropdown"
              role="dialog"
              aria-modal="true"
              aria-label="Merge options"
              class="absolute right-0 top-full mt-2 w-80 bg-slate-900 border border-slate-800 rounded-lg shadow-xl z-50"
            >
              <div class="p-3 border-b border-slate-800">
                <div class="flex items-center justify-between">
                  <span class="text-xs text-slate-400">Merge Method</span>
                  <span :class="['text-xs font-mono', mergeableStatusColor]">{{ mergeableStatusText }}</span>
                </div>
              </div>

              <div class="p-2">
                <label
                  v-for="method in availableMergeMethods"
                  :key="method.value"
                  class="flex items-start gap-3 p-2 rounded-lg cursor-pointer hover:bg-slate-800 transition-colors"
                >
                  <input
                    type="radio"
                    :value="method.value"
                    v-model="selectedMergeMethod"
                    class="mt-1 accent-slate-300"
                  />
                  <div class="flex-1 min-w-0">
                    <div class="text-sm text-slate-200">{{ method.label }}</div>
                    <div class="text-xs text-slate-400 mt-0.5">{{ method.description }}</div>
                  </div>
                </label>
              </div>

              <div v-if="mergeError" class="px-3 py-2 border-t border-slate-800">
                <p class="text-xs text-red-400">{{ mergeError }}</p>
              </div>

              <div class="p-3 border-t border-slate-800 flex justify-end gap-2">
                <button
                  @click="showMergeDropdown = false; mergeError = null"
                  class="px-3 py-1.5 text-xs text-slate-400 hover:text-slate-200 transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="handleMerge"
                  :disabled="!canMerge || merging"
                  class="px-4 py-1.5 bg-slate-200 hover:bg-white rounded text-xs text-slate-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {{ merging ? 'Merging...' : 'Confirm merge' }}
                </button>
              </div>
            </div>
          </div>

          <button
            @click="toggleCommentsPanel"
            :class="[
              'p-2 rounded-lg relative transition-all duration-150',
              commentsPanel
                ? 'bg-slate-200 text-slate-900'
                : 'border border-slate-800 text-slate-300 hover:bg-slate-800'
            ]"
            title="Comments"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
            </svg>
            <span
              v-if="unresolvedThreadsCount > 0"
              class="absolute -top-1.5 -right-1.5 bg-slate-200 text-slate-900 text-[10px] rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-0.5 font-mono tabular-nums"
            >
              {{ unresolvedThreadsCount }}
            </span>
          </button>

          <button
            @click="refreshFileViewStates"
            :disabled="refreshingViewStates"
            class="hidden sm:flex px-3 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-xs text-slate-300 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed items-center gap-2"
            title="Refresh file viewed states from GitHub"
          >
            <svg
              :class="['w-3.5 h-3.5', { 'animate-spin': refreshingViewStates }]"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span>{{ refreshingViewStates ? 'Refreshing...' : 'Refresh' }}</span>
          </button>

          <a
            :href="prDetail?.url"
            target="_blank"
            rel="noopener noreferrer"
            class="px-3 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-xs text-slate-300 transition-all duration-150"
          >
            GitHub
          </a>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center">
        <svg class="animate-spin h-16 w-16 mx-auto text-slate-400 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-slate-400 text-sm">Loading PR details...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center max-w-md">
        <svg class="w-20 h-20 mx-auto text-red-500 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h2 class="text-2xl text-slate-200 mb-3">Failed to load PR</h2>
        <p class="text-slate-400 mb-6 text-sm">{{ error }}</p>
        <button
          @click="fetchPRDetail(props.id)"
          class="px-5 py-2.5 bg-slate-200 hover:bg-white rounded-lg text-slate-900 transition-all duration-150"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- Main Content - Full Page Layout -->
    <div v-else-if="prDetail" class="flex flex-1">
      <div
        v-if="preferences.fileTreeVisible"
        :style="{ width: `${fileTreeWidth}px` }"
        class="hidden md:block flex-shrink-0 bg-slate-900/40 border-r border-slate-800 relative sticky top-[6.75rem] h-[calc(100vh-6.75rem)]"
      >
        <FileTree
          :files="prDetail.files"
          :selected-file="selectedFile || undefined"
          :viewed-files="viewedFileSet"
          @select-file="scrollToFile"
          @toggle-viewed="handleToggleViewed"
        />
        <!-- Resize Handle -->
        <div
          class="absolute top-0 right-0 w-1.5 h-full cursor-ew-resize bg-slate-800 hover:bg-slate-600 transition-colors duration-150"
          @mousedown="startResizeFileTree"
        />
      </div>

      <!-- Main Content Area -->
      <div class="flex-1 flex flex-col min-w-0">
        <!-- Top Section: PR Info -->
        <div class="border-b border-slate-800">
          <div class="flex flex-col lg:flex-row gap-4 lg:gap-6 p-4 sm:p-6">
            <div class="flex-1 min-w-0">
              <!-- Branch Info -->
              <div class="p-4 border border-slate-800 rounded mb-4">
                <div class="text-xs text-slate-400 mb-2 uppercase tracking-wider">Branches</div>
                <div class="flex flex-wrap items-center gap-2 text-sm">
                  <span class="text-emerald-400 font-mono truncate max-w-[200px] sm:max-w-none">{{ prDetail.sourceBranch }}</span>
                  <svg class="w-4 h-4 text-slate-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                  <span class="text-blue-400 font-mono truncate max-w-[200px] sm:max-w-none">{{ prDetail.targetBranch }}</span>
                </div>
              </div>

              <!-- Description -->
              <div class="p-4 border border-slate-800 rounded">
                <div class="flex items-center justify-between mb-3">
                  <div class="text-xs font-semibold text-slate-300 uppercase tracking-wider">Description</div>
                  <div v-if="prDetail?.isMerged" class="text-xs text-slate-500 flex items-center gap-1 font-mono">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                    Merged - read only
                  </div>
                </div>
                <EditableDescription
                  v-if="prDetail"
                  :content="prDetail.description || ''"
                  :pr-id="prDetail.id"
                  @save="handleDescriptionSave"
                />
              </div>

              <!-- General Comments -->
              <GeneralComments
                v-if="prDetail"
                :comments="prDetail.allComments"
                :current-username="authStore.username"
                :is-merged="prDetail.isMerged"
                :pr-id="prDetail.id"
                @add-comment="handleAddGeneralComment"
                @update-comment="handleUpdateGeneralComment"
                @delete-comment="handleDeleteGeneralComment"
                class="mt-4"
              />
            </div>

            <!-- Right: Info & Stats -->
            <div class="lg:w-96 flex-shrink-0 space-y-4">
                <!-- Priority -->
                <div class="p-4 border border-slate-800 rounded">
                  <div class="flex items-center justify-between gap-2">
                    <div class="text-sm font-semibold text-slate-300 uppercase tracking-wider">Priority</div>
                    <span
                      v-if="prDetail.priorityOverridden"
                      class="text-[10px] uppercase tracking-wider text-amber-400/80"
                      title="Manually overridden (auto-derivation disabled)"
                    >override</span>
                    <span
                      v-else
                      class="text-[10px] uppercase tracking-wider text-slate-500"
                      title="Priority auto-derived from PR signals"
                    >auto</span>
                  </div>
                  <div class="flex items-center gap-2 mt-2">
                    <span
                      class="text-xs font-mono"
                      :class="getPriorityColor(prDetail.priority ?? 1)"
                    >{{ getPriorityGlyph(prDetail.priority ?? 1) }}</span>
                    <select
                      :value="prDetail.priorityOverridden ? prDetail.priority : 'auto'"
                      :disabled="settingPriority"
                      @change="handlePriorityChange(($event.target as HTMLSelectElement).value)"
                      class="flex-1 bg-slate-900/60 border border-slate-700 rounded px-2 py-1.5 text-sm text-slate-200 focus:outline-none focus:border-slate-600 transition-colors disabled:opacity-50"
                    >
                      <option value="auto">Auto (derived)</option>
                      <option :value="PRIORITY_LOW">Low</option>
                      <option :value="PRIORITY_NORMAL">Normal</option>
                      <option :value="PRIORITY_HIGH">High</option>
                      <option :value="PRIORITY_URGENT">Urgent</option>
                    </select>
                  </div>
                </div>

                <!-- Reviewer Manager -->
                <div class="p-4 border border-slate-800 rounded">
                  <ReviewerManager
                    :pull-request-id="prDetail.id"
                    @error="handleReviewerError"
                  />
                </div>

                <!-- Review Timeline -->
                <div class="p-4 border border-slate-800 rounded">
                  <ReviewTimeline
                    :pull-request-id="prDetail.id"
                    @error="handleTimelineError"
                  />
                </div>

                <!-- CI/CD Checks -->
                <div v-if="prDetail.checksStatus || (prDetail.checkRuns && prDetail.checkRuns.length > 0)" class="p-4 border border-slate-800 rounded">
                  <div class="text-xs text-slate-400 mb-3 uppercase tracking-wider">CI/CD Checks</div>

                  <div v-if="prDetail.checksStatus" class="space-y-2">
                    <div class="flex items-center gap-2 p-2 rounded bg-slate-800/40">
                      <CIBadge :status="prDetail.checksStatus" :show-count="true" :total-count="prDetail.checkRuns?.length || 0" />
                      <span class="text-sm text-slate-200">{{ checksStatusLabel }}</span>
                    </div>

                    <div v-if="prDetail.checkRuns && prDetail.checkRuns.length > 0" class="mt-3 space-y-1 max-h-48 overflow-y-auto">
                      <div
                        v-for="check in prDetail.checkRuns"
                        :key="check.id"
                        class="flex items-center gap-2 p-2 rounded hover:bg-slate-800/60 transition-colors duration-150"
                      >
                        <CIBadge
                          :status="getCheckStatusFromCheckRun(check)"
                          :show-count="false"
                          :compact="true"
                        />
                        <span class="text-xs text-slate-200 truncate flex-1 font-mono">{{ check.name }}</span>
                        <a
                          v-if="check.url"
                          :href="check.url"
                          target="_blank"
                          rel="noopener noreferrer"
                          class="text-slate-400 hover:text-slate-200 transition-colors duration-150"
                          title="View on GitHub"
                        >
                          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                          </svg>
                        </a>
                      </div>
                    </div>
                  </div>
                </div>
            </div>
          </div>
        </div>

        <!-- Bottom Section: File Diffs -->
        <div class="flex-1">
          <div class="px-3 sm:px-6 py-4 sm:py-5">
                <div class="flex items-center justify-between py-2">
                  <h2 class="text-sm font-semibold text-slate-300 uppercase tracking-wider">
                    Files Changed <span class="text-slate-500 font-normal font-mono tabular-nums">({{ prDetail.files.length }})</span>
                  </h2>
                  <div class="flex items-center gap-2">
                    <button
                      @click="toggleFileTree"
                      class="hidden md:flex items-center gap-2 px-3 py-2 border border-slate-800 rounded-lg hover:bg-slate-800 text-slate-300 text-xs transition-all duration-150"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M4 6h16M4 12h16M4 18h16" />
                      </svg>
                      <span>{{ preferences.fileTreeVisible ? 'Hide' : 'Show' }} Tree</span>
                    </button>
                    <div class="relative">
                      <button
                        @click="showSettingsDropdown = !showSettingsDropdown"
                        class="p-2 border border-slate-800 rounded-lg hover:bg-slate-800 text-slate-300 transition-all duration-150"
                        title="Diff settings"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        </svg>
                      </button>
                      <Transition
                        enter-active-class="transition-opacity duration-200"
                        enter-from-class="opacity-0"
                        enter-to-class="opacity-100"
                        leave-active-class="transition-opacity duration-200"
                        leave-from-class="opacity-100"
                        leave-to-class="opacity-0"
                      >
                        <div
                          v-if="showSettingsDropdown"
                          class="absolute right-0 top-full mt-2 bg-slate-900 border border-slate-800 rounded-lg shadow-xl z-50 min-w-[220px] p-2"
                        >
                          <div class="space-y-1">
                            <div class="text-xs font-semibold text-slate-400 px-3 py-2 uppercase tracking-wider">Diff View</div>
                            <button
                              @click="setDiffViewMode('unified'); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                                preferences.diffViewMode === 'unified'
                                  ? 'bg-slate-800 text-slate-100 border border-slate-700'
                                  : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                              ]"
                            >
                              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
                              </svg>
                              Unified
                            </button>
                            <button
                              @click="setDiffViewMode('split'); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                                preferences.diffViewMode === 'split'
                                  ? 'bg-slate-800 text-slate-100 border border-slate-700'
                                  : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                              ]"
                            >
                              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 4v16M4 4h16" />
                              </svg>
                              Split
                            </button>
                            <button
                              @click="toggleContext(); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2 rounded text-xs text-left transition-all duration-150',
                                preferences.showContext
                                  ? 'bg-slate-800 text-slate-100 border border-slate-700'
                                  : 'text-slate-300 hover:bg-slate-800/60 border border-transparent'
                              ]"
                            >
                              <svg
                                v-if="preferences.showContext"
                                class="w-4 h-4 flex-shrink-0"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                              >
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                              </svg>
                              <svg
                                v-else
                                class="w-4 h-4 flex-shrink-0"
                                fill="none"
                                stroke="currentColor"
                                viewBox="0 0 24 24"
                              >
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542 7z" />
                              </svg>
                              {{ preferences.showContext ? 'Hide Context' : 'Show Context' }}
                            </button>
                          </div>
                        </div>
                      </Transition>
                    </div>
                  </div>
                </div>

                <!-- File Diffs -->
                <FileDiffViewer
                  v-for="file in prDetail.files"
                  :key="file.path"
                  :ref="el => setFileRef(file.path!, el)"
                  :file="file"
                  :viewed="isFileViewed(file.path!)"
                  :comments="prDetail.allComments"
                  :review-threads="prDetail.reviewThreads"
                  :pending-review-comments="prDetail.pendingReview?.comments"
                  :current-username="authStore.username"
                  :on-add-comment="(line: number, body: string, side: string) => handleAddComment(file.path!, line, body, side)"
                  :on-delete-pending-comment="handleDeletePendingComment"
                  :on-reply-to-thread="(threadId: string, line: number, body: string) => handleReplyToThread(threadId, line, body)"
                  :on-resolve-thread="(threadId: string, resolved: boolean) => handleResolveThread(threadId, resolved)"
                  :on-edit-comment="handleEditComment"
                  :on-delete-comment="handleDeleteComment"
                  :on-toggle-viewed="handleToggleViewed"
                  :initial-expanded="!isFileViewed(file.path!)"
                  :pr-id="prDetail.id"
                  class="mb-4"
                />
          </div>
        </div>
      </div>

      <Transition
        enter-active-class="transition-opacity duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition-opacity duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="commentsPanel"
          class="fixed inset-0 bg-black/50 z-20 md:hidden"
          @click="toggleCommentsPanel"
        />
      </Transition>
      <Transition
        enter-active-class="transition-transform duration-300 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-200 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="commentsPanel"
          class="fixed right-0 top-0 md:top-20 z-30 h-screen md:h-[calc(100vh-5rem)] w-[90%] sm:w-80 md:w-[320px]"
          :style="{ width: commentsPanelWidth ? `${commentsPanelWidth}px` : undefined }"
        >
          <CommentsPanel
            :comments="prDetail?.allComments || []"
            :review-threads="prDetail?.reviewThreads || []"
            :pr-id="id"
            :current-username="authStore.username"
            @close="toggleCommentsPanel"
            @scroll-to-comment="scrollToComment"
            @scroll-to-thread="scrollToThread"
            @reply-added="handleReplyAdded"
            @thread-resolved="handleThreadResolved"
          />
          <div
            class="hidden md:block absolute top-0 left-0 w-1.5 h-full cursor-ew-resize bg-slate-800 hover:bg-slate-600 transition-colors duration-150"
            @mousedown="startResizeCommentsPanel"
          />
        </div>
      </Transition>
    </div>

    <!-- Review Modal -->
    <Transition
      enter-active-class="transition-opacity duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="showReviewModal"
        role="dialog"
        aria-modal="true"
        aria-label="Review pull request"
        class="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4"
        @click.self="showReviewModal = false"
      >
        <div class="bg-slate-900 border border-slate-800 rounded-lg p-6 max-w-lg w-full shadow-2xl shadow-black/50 max-h-[90vh] overflow-y-auto">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg text-slate-100">Review PR</h3>
            <div v-if="prDetail?.pendingReview?.comments?.length" class="flex items-center gap-2">
              <span class="px-2 py-1 text-xs bg-amber-950/30 text-amber-400 rounded border border-amber-900/50 font-mono tabular-nums">
                {{ prDetail.pendingReview.comments.length }} pending comment{{ prDetail.pendingReview.comments.length !== 1 ? 's' : '' }}
              </span>
            </div>
          </div>

          <!-- Pending Review Comments -->
          <div v-if="prDetail?.pendingReview?.comments?.length" class="mb-4 border border-slate-800 rounded overflow-hidden">
            <div class="px-4 py-2 bg-slate-800/40 border-b border-slate-800">
              <span class="text-xs text-slate-400 uppercase tracking-wider">Your Draft Comments</span>
            </div>
            <div class="divide-y divide-slate-800 max-h-48 overflow-y-auto">
              <div
                v-for="comment in prDetail.pendingReview.comments"
                :key="comment.gitHubId"
                class="p-3 hover:bg-slate-800/40 group"
              >
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 text-xs text-slate-400 mb-1 font-mono">
                      <span class="truncate">{{ comment.path }}</span>
                      <span v-if="comment.line" class="text-slate-500">:{{ comment.line }}</span>
                    </div>
                    <p class="text-sm text-slate-200 line-clamp-2">{{ comment.body }}</p>
                  </div>
                  <button
                    @click="handleDeletePendingComment(comment.gitHubId)"
                    class="opacity-0 group-hover:opacity-100 p-1 hover:bg-rose-500/20 rounded transition-all"
                    title="Delete comment"
                  >
                    <svg class="w-4 h-4 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-3 gap-2 mb-4">
            <button
              @click="reviewAction = 'APPROVED'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs transition-all duration-150 border',
                reviewAction === 'APPROVED'
                  ? 'bg-slate-200 border-slate-200 text-slate-900'
                  : 'border-slate-800 text-slate-300 hover:bg-slate-800'
              ]"
            >
              Approve
            </button>
            <button
              @click="reviewAction = 'CHANGES_REQUESTED'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs transition-all duration-150 border',
                reviewAction === 'CHANGES_REQUESTED'
                  ? 'bg-slate-200 border-slate-200 text-slate-900'
                  : 'border-slate-800 text-slate-300 hover:bg-slate-800'
              ]"
            >
              Changes
            </button>
            <button
              @click="reviewAction = 'COMMENT'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs transition-all duration-150 border',
                reviewAction === 'COMMENT'
                  ? 'bg-slate-200 border-slate-200 text-slate-900'
                  : 'border-slate-800 text-slate-300 hover:bg-slate-800'
              ]"
            >
              Comment
            </button>
          </div>

          <textarea
            v-model="reviewComment"
            :placeholder="reviewAction === 'APPROVED' ? 'Add your approval comment (optional)...' : reviewAction === 'CHANGES_REQUESTED' ? 'Describe changes requested... (required)' : 'Add your comment... (required)'"
            :class="[
              'w-full px-4 py-3 border rounded text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 mb-4 transition-all duration-150 placeholder:text-slate-500',
              (reviewAction === 'CHANGES_REQUESTED' || reviewAction === 'COMMENT') && !reviewComment.trim()
                ? 'bg-slate-900 border-rose-500 focus:ring-rose-500/40 focus:border-rose-500'
                : 'bg-slate-900 border-slate-800 focus:ring-slate-500/40 focus:border-slate-600'
            ]"
            rows="4"
          />
          <div v-if="reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim()" class="text-rose-400 text-xs mb-4">
            Please provide details about the changes you're requesting.
          </div>
          <div v-if="reviewAction === 'COMMENT' && !reviewComment.trim()" class="text-rose-400 text-xs mb-4">
            Please provide a comment.
          </div>
          <div class="flex gap-3 justify-end">
            <button
              v-if="prDetail?.pendingReview?.comments?.length"
              @click="handleDiscardPendingReview"
              class="px-4 py-2 bg-rose-500/10 hover:bg-rose-500/20 rounded-lg text-sm text-rose-400 transition-all duration-150 border border-rose-500/30"
            >
              Discard Draft
            </button>
            <div class="flex-1"></div>
            <button
              @click="showReviewModal = false"
              class="px-4 py-2 border border-slate-800 hover:bg-slate-800 rounded-lg text-sm text-slate-300 transition-all duration-150"
            >
              Cancel
            </button>
            <button
              @click="handleSubmitReview"
              :disabled="submittingReview || ((reviewAction === 'CHANGES_REQUESTED' || reviewAction === 'COMMENT') && !reviewComment.trim())"
              :class="[
                'px-5 py-2 rounded-lg text-sm transition-all duration-150',
                'bg-slate-200 hover:bg-white text-slate-900',
                { 'opacity-50 cursor-not-allowed': submittingReview || ((reviewAction === 'CHANGES_REQUESTED' || reviewAction === 'COMMENT') && !reviewComment.trim()) }
              ]"
            >
              {{ submittingReview ? 'Submitting...' : (prDetail?.pendingReview?.comments?.length ? 'Submit Review' : 'Submit') }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
 import { ref, onMounted, onUnmounted, computed, nextTick } from 'vue';
 import { usePRDetail } from '../composables/usePRDetail';
 import { useUserPreferences } from '../composables/useUserPreferences';
 import { useSignalR, type CommentNotification, type ThreadNotification, type CheckRunsNotification, type PRPriorityNotification } from '../composables/useSignalR';
  import { apiService } from '../services/api';
  import FileDiffViewer from '../components/FileDiffViewer.vue';
  import FileTree from '../components/FileTree.vue';
  import CommentsPanel from '../components/CommentsPanel.vue';
  import GeneralComments from '../components/GeneralComments.vue';
  import StatusBadge from '../components/StatusBadge.vue';
  import Breadcrumb from '../components/Breadcrumb.vue';
 import CIBadge from '../components/CIBadge.vue';
 import EditableDescription from '../components/EditableDescription.vue';
 import ReviewerManager from '../components/ReviewerManager.vue';
 import ReviewTimeline from '../components/ReviewTimeline.vue';
  import type { Comment, CheckRun } from '../types';
  import { useAuthStore } from '../stores/auth';
  import { useToast } from '../composables/useToast';
  import {
    getPriorityColor,
    getPriorityGlyph,
    PRIORITY_LOW,
    PRIORITY_NORMAL,
    PRIORITY_HIGH,
    PRIORITY_URGENT,
  } from '../utils/prHelpers';

 const authStore = useAuthStore();
 const signalR = useSignalR();
 const toast = useToast();

const props = defineProps<{
  id: number;
}>();

const {
  prDetail,
  loading,
  error,
  commentsPanel,
  fetchPRDetail,
  addPendingReviewComment,
  deletePendingReviewComment,
  addReply,
  submitReview,
  submitPendingReview,
  deletePendingReview,
  mergePR,
  getMergeOptions,
  publishDraftPR,
  toggleCommentsPanel,
  toggleFileTree,
  editComment,
  deleteComment,
  updatePR,
  addGeneralComment,
  updateGeneralComment,
  deleteGeneralComment,
} = usePRDetail();

const { preferences, loadPreferences, setFileTreeWidth, setCommentsPanelWidth, updatePreferences, setDiffViewMode, setShowContext } = useUserPreferences();

const selectedFile = ref<string | null>(null);
const showReviewModal = ref(false);
const showSettingsDropdown = ref(false);
const reviewAction = ref<'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'>('COMMENT');
const reviewComment = ref('');
const submittingReview = ref(false);
const publishingDraft = ref(false);
const merging = ref(false);
const showMergeDropdown = ref(false);
const mergeOptions = ref<{
  mergeCommitAllowed: boolean;
  squashMergeAllowed: boolean;
  rebaseMergeAllowed: boolean;
  defaultMergeMethod: string;
  mergeableState?: string;
  isMerged: boolean;
  isDraft: boolean;
} | null>(null);
const selectedMergeMethod = ref<'merge' | 'squash' | 'rebase'>('squash');
const mergeError = ref<string | null>(null);
const fileRefs = ref<Map<string, any>>(new Map());
const refreshingViewStates = ref(false);
const settingPriority = ref(false);

const isEditingTitle = ref(false);
const editTitleValue = ref('');
const savingTitle = ref(false);
const titleInputRef = ref<HTMLInputElement | null>(null);

// Resizable widths
const fileTreeWidth = ref(256);
const commentsPanelWidth = ref(320);
const isResizingFileTree = ref(false);
const isResizingComments = ref(false);

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    return;
  }

  await loadPreferences();
  fileTreeWidth.value = preferences.value.fileTreeWidth;
  commentsPanelWidth.value = preferences.value.commentsPanelWidth;
  
  await fetchPRDetail(props.id);

  await refreshFileViewStates();

  if (preferences.value.viewedFilesByPr && preferences.value.viewedFilesByPr[props.id]) {
    if (prDetail.value) {
      prDetail.value.viewedFiles = preferences.value.viewedFilesByPr[props.id];
    }
  }
  
  document.addEventListener('mousemove', handleMouseMove);
  document.addEventListener('mouseup', handleMouseUp);
  document.addEventListener('click', handleClickOutside);

  if (authStore.token) {
    await signalR.connect(authStore.token);
    await signalR.joinPRRoom(props.id);
    setupSignalRHandlers();
  }
});

const setupSignalRHandlers = () => {
  signalR.onCommentChanged.value = (notification: CommentNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;
    
    if (notification.action === 'added') {
      const exists = prDetail.value.allComments.some(c => c.id === notification.comment.id);
      if (!exists) {
        prDetail.value.allComments.push(notification.comment);
      }
    } else if (notification.action === 'updated') {
      const idx = prDetail.value.allComments.findIndex(c => c.id === notification.comment.id);
      if (idx !== -1) {
        prDetail.value.allComments[idx] = notification.comment;
      }
    } else if (notification.action === 'deleted') {
      prDetail.value.allComments = prDetail.value.allComments.filter(c => c.id !== notification.comment.id);
    }
  };

  signalR.onThreadChanged.value = (notification: ThreadNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;
    
    const threadIdx = prDetail.value.reviewThreads.findIndex(t => t.id === notification.threadId);
    if (threadIdx !== -1 && prDetail.value.reviewThreads[threadIdx]) {
      prDetail.value.reviewThreads[threadIdx].isResolved = notification.isResolved;
      prDetail.value.reviewThreads[threadIdx].state = notification.isResolved ? 'RESOLVED' : 'UNRESOLVED';
    }
  };

  signalR.onCheckRunsUpdated.value = (notification: CheckRunsNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;

    prDetail.value.checksStatus = notification.checksStatus as any;
    if (notification.checkRuns) {
      prDetail.value.checkRuns = notification.checkRuns.map(cr => ({
        id: cr.id,
        gitHubId: cr.gitHubId,
        name: cr.name,
        status: cr.status,
        conclusion: cr.conclusion,
        url: cr.url,
        startedAt: cr.startedAt,
        completedAt: cr.completedAt
      }));
    }
  };

  signalR.onPRPriorityChanged.value = (notification: PRPriorityNotification) => {
    if (notification.pullRequestId !== props.id || !prDetail.value) return;
    prDetail.value.priority = notification.priority;
    prDetail.value.priorityOverridden = notification.overridden;
  };
};

const toggleContext = async () => {
  console.log('Toggle context - current value:', preferences.value.showContext);
  const newValue = !preferences.value.showContext;
  console.log('Toggle context - new value:', newValue);
  try {
    await setShowContext(newValue);
    console.log('Toggle context - after update:', preferences.value.showContext);
  } catch (error) {
    console.error('Error toggling context:', error);
    toast.error('Failed to toggle context. Please try again.');
  }
};

onUnmounted(async () => {
  document.removeEventListener('mousemove', handleMouseMove);
  document.removeEventListener('mouseup', handleMouseUp);
  document.removeEventListener('click', handleClickOutside);
  
  await signalR.leavePRRoom(props.id);
  await signalR.disconnect();
});

// Resize handlers
const startResizeFileTree = () => {
  isResizingFileTree.value = true;
};

const startResizeCommentsPanel = () => {
  isResizingComments.value = true;
};

const handleMouseMove = (e: MouseEvent) => {
  if (isResizingFileTree.value) {
    const newWidth = Math.max(200, Math.min(600, e.clientX));
    fileTreeWidth.value = newWidth;
  }
  if (isResizingComments.value) {
    const newWidth = Math.max(250, Math.min(800, window.innerWidth - e.clientX));
    commentsPanelWidth.value = newWidth;
  }
};

const handleMouseUp = async () => {
  if (isResizingFileTree.value) {
    await setFileTreeWidth(fileTreeWidth.value);
    isResizingFileTree.value = false;
  }
  if (isResizingComments.value) {
    await setCommentsPanelWidth(commentsPanelWidth.value);
    isResizingComments.value = false;
  }
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (showSettingsDropdown.value && !target.closest('.relative')) {
    showSettingsDropdown.value = false;
  }
  if (showMergeDropdown.value && !target.closest('.relative')) {
    showMergeDropdown.value = false;
    mergeError.value = null;
  }
};

  const handleAddComment = async (path: string, line: number, body: string, side: string = 'RIGHT') => {
    const comment = await addPendingReviewComment(props.id, { path, line, body, side });
    if (comment) {
      console.log('Pending review comment added successfully');
    }
  };

  const handleReplyToThread = async (threadId: string, _line: number, body: string) => {
    const reply = await addReply(props.id, { reviewThreadId: threadId, body });
    if (reply) {
      console.log('Reply added successfully');
    }
  };

  const handleResolveThread = async (threadId: string, resolved: boolean) => {
    try {
      if (resolved) {
        await apiService.resolveReviewThread(props.id, threadId, true);
        console.log('Thread resolved successfully');
      } else {
        await apiService.unresolveReviewThread(props.id, threadId);
        console.log('Thread unresolved successfully');
      }
      
      if (prDetail.value) {
        const thread = prDetail.value.reviewThreads.find(t => t.gitHubId === threadId);
        if (thread) {
          thread.isResolved = resolved;
          thread.state = resolved ? 'RESOLVED' : 'UNRESOLVED';
        }
      }
    } catch (error) {
      console.error('Failed to resolve/unresolve thread:', error);
    }
  };

  const handleReplyAdded = (comment: Comment) => {
    if (prDetail.value && comment) {
      const existingComment = prDetail.value.allComments.find(c => c.id === comment.id);
      if (!existingComment) {
        prDetail.value.allComments.push(comment);
        
        const thread = prDetail.value.reviewThreads.find(
          t => t.gitHubId === comment.reviewThreadId?.toString() || String(t.id) === comment.reviewThreadId?.toString()
        );
        if (thread) {
          thread.commentCount = (thread.commentCount || 0) + 1;
        }
      }
    }
  };

  const handleThreadResolved = (threadId: string, resolved: boolean) => {
    if (prDetail.value) {
      const thread = prDetail.value.reviewThreads.find(t => t.gitHubId === threadId);
      if (thread) {
        thread.isResolved = resolved;
        thread.state = resolved ? 'RESOLVED' : 'UNRESOLVED';
      }
      
      const hasUnresolved = prDetail.value.reviewThreads.some(t => !t.isResolved && !t.isOutdated);
      prDetail.value.hasUnresolvedThreads = hasUnresolved;
    }
  };

  const handleAddGeneralComment = async (body: string) => {
    await addGeneralComment(props.id, body);
  };

  const handleUpdateGeneralComment = async (gitHubId: string, body: string) => {
    await updateGeneralComment(props.id, gitHubId, body);
  };

  const handleDeleteGeneralComment = async (gitHubId: string) => {
    await deleteGeneralComment(props.id, gitHubId);
  };

  const handleReviewerError = (message: string) => {
    console.error('Reviewer error:', message);
  };

  const handlePriorityChange = async (value: string) => {
    if (!prDetail.value) return;
    const priority = value === 'auto' ? null : Number(value);
    settingPriority.value = true;
    try {
      const result = await apiService.setPRPriority(props.id, priority);
      prDetail.value.priority = result.priority;
      prDetail.value.priorityOverridden = result.overridden;
      toast.success(value === 'auto' ? 'Priority set to auto (derived)' : `Priority set to ${value === '0' ? 'Low' : value === '1' ? 'Normal' : value === '2' ? 'High' : 'Urgent'}`);
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Failed to set priority');
    } finally {
      settingPriority.value = false;
    }
  };

  const handleTimelineError = (message: string) => {
    console.error('Timeline error:', message);
  };


const handleSubmitReview = async () => {
  if (reviewAction.value === 'CHANGES_REQUESTED' && !reviewComment.value.trim()) {
    toast.error('Please provide details about the changes you are requesting.');
    return;
  }

  if (reviewAction.value === 'COMMENT' && !reviewComment.value.trim()) {
    toast.error('Please provide a comment when submitting a comment review.');
    return;
  }

  submittingReview.value = true;
  
  let success: boolean;
  if (prDetail.value?.pendingReview && prDetail.value.pendingReview.comments.length > 0) {
    success = await submitPendingReview(props.id, {
      state: reviewAction.value,
      body: reviewComment.value || undefined,
    });
  } else {
    success = await submitReview(props.id, {
      state: reviewAction.value,
      body: reviewComment.value || undefined,
    });
  }
  
  submittingReview.value = false;

  if (success) {
    showReviewModal.value = false;
    reviewComment.value = '';
    toast.success('Review submitted successfully');
  }
};

const handleDiscardPendingReview = async () => {
  if (!prDetail.value?.pendingReview) return;
  
  if (!confirm('Are you sure you want to discard your pending review? All draft comments will be deleted.')) {
    return;
  }
  
  const success = await deletePendingReview(props.id);
  if (success) {
    showReviewModal.value = false;
    reviewComment.value = '';
  }
};

const handleDeletePendingComment = async (commentId: string) => {
  await deletePendingReviewComment(props.id, commentId);
};

const handlePublishDraft = async () => {
  if (!prDetail.value?.draft) return;

  publishingDraft.value = true;
  const success = await publishDraftPR(props.id);
  publishingDraft.value = false;

  if (!success) {
    toast.error('Failed to publish draft PR');
  } else {
    toast.success('Draft PR published successfully');
  }
};

const loadMergeOptions = async () => {
  const options = await getMergeOptions(props.id);
  if (options) {
    mergeOptions.value = options;
    selectedMergeMethod.value = options.defaultMergeMethod as 'merge' | 'squash' | 'rebase';
  }
};

const unresolvedThreadsCount = computed(() => {
  if (!prDetail.value?.reviewThreads) return 0;
  return prDetail.value.reviewThreads.filter(t => !t.isResolved).length;
});

const canMerge = computed(() => {
  if (!prDetail.value) return false;
  if (prDetail.value.draft) return false;
  if (prDetail.value.isMerged) return false;
  if (prDetail.value.mergeableState === 'CONFLICTING') return false;
  if (prDetail.value.requiredApprovingReviews !== undefined && !prDetail.value.isMergeReady) return false;
  return true;
});

const mergeableStatusText = computed(() => {
  if (!prDetail.value) return '';
  if (prDetail.value.isMerged) return 'Already merged';
  if (prDetail.value.draft) return 'Draft PR';
  if (prDetail.value.mergeableState === 'CONFLICTING') return 'Has conflicts';
  if (prDetail.value.mergeableState === 'UNKNOWN') return 'Checking...';
  if (prDetail.value.mergeBlockReason) return prDetail.value.mergeBlockReason;
  if (prDetail.value.isMergeReady) return 'Ready to merge';
  return 'Review required';
});

const mergeableStatusColor = computed(() => {
  if (!prDetail.value) return 'text-slate-400';
  if (prDetail.value.isMerged) return 'text-emerald-400';
  if (prDetail.value.draft) return 'text-amber-400';
  if (prDetail.value.mergeableState === 'CONFLICTING') return 'text-red-400';
  if (prDetail.value.mergeableState === 'UNKNOWN') return 'text-slate-400';
  if (prDetail.value.isMergeReady) return 'text-emerald-400';
  if (prDetail.value.mergeBlockReason) return 'text-amber-400';
  return 'text-slate-400';
});

const handleMerge = async () => {
  if (!canMerge.value) return;

  merging.value = true;
  mergeError.value = null;

  const result = await mergePR(props.id, {
    mergeMethod: selectedMergeMethod.value,
  });

  merging.value = false;
  showMergeDropdown.value = false;

  if (!result.success) {
    mergeError.value = result.message || 'Failed to merge PR';
    toast.error(result.message || 'Failed to merge PR');
  } else {
    toast.success('PR merged successfully');
  }
};

const availableMergeMethods = computed(() => {
  if (!mergeOptions.value) return [];
  const methods: { value: string; label: string; description: string }[] = [];
  
  if (mergeOptions.value.squashMergeAllowed) {
    methods.push({
      value: 'squash',
      label: 'Squash and merge',
      description: 'All commits from this branch will be combined into one commit in the base branch.',
    });
  }
  if (mergeOptions.value.mergeCommitAllowed) {
    methods.push({
      value: 'merge',
      label: 'Create a merge commit',
      description: 'All commits from this branch will be added to the base branch via a merge commit.',
    });
  }
  if (mergeOptions.value.rebaseMergeAllowed) {
    methods.push({
      value: 'rebase',
      label: 'Rebase and merge',
      description: 'All commits from this branch will be rebased and added to the base branch.',
    });
  }
  return methods;
});

const scrollToFile = (path: string, line?: number) => {
  selectedFile.value = path;
  const fileRef = fileRefs.value.get(path);
  if (fileRef && fileRef.$el) {
    // Ensure file is expanded
    if (line && fileRef.expanded !== undefined && !fileRef.expanded) {
      fileRef.expanded = true;
      // Wait for expansion before scrolling
      setTimeout(() => {
        fileRef.$el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        if (fileRef.highlightLine) {
          fileRef.highlightLine(line);
        }
      }, 100);
    } else {
      fileRef.$el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      if (line && fileRef.highlightLine) {
        fileRef.highlightLine(line);
      }
    }
  }
};

const scrollToComment = (comment: Comment, line?: number) => {
  if (comment.path) {
    scrollToFile(comment.path, line ?? undefined);
  }
};

const scrollToThread = async (threadId: string, line?: number) => {
  const thread = prDetail.value?.reviewThreads.find(rt => rt.gitHubId === threadId);
  if (!thread?.path) return;

  const fileRef = fileRefs.value.get(thread.path);
  if (fileRef) {
    if (fileRef.expanded !== undefined && !fileRef.expanded) {
      fileRef.expanded = true;
      await nextTick();
    }

    if (fileRef.scrollToThread) {
      await nextTick();
      fileRef.scrollToThread(threadId);
    } else {
      scrollToFile(thread.path, line ?? undefined);
    }
  }
};

const setFileRef = (path: string, el: any) => {
  if (el) {
    fileRefs.value.set(path, el);
  }
};

const saveViewedFiles = async (filePath?: string, viewed?: boolean) => {
  if (!prDetail.value) return;

  const viewedFilesByPr = preferences.value.viewedFilesByPr || {};
  viewedFilesByPr[props.id] = prDetail.value.viewedFiles?.filter(f => f !== undefined) || [];

  await updatePreferences({ viewedFilesByPr });

  // Sync with GitHub using GraphQL
  if (filePath !== undefined && viewed !== undefined) {
    try {
      await apiService.updateFileViewedState(props.id, filePath, viewed);
    } catch (error) {
      console.error('Failed to sync viewed file to GitHub:', error);
    }
  }
};

const refreshFileViewStates = async () => {
  if (!prDetail.value || refreshingViewStates.value) return;
  
  refreshingViewStates.value = true;
  try {
    const updatedFiles = await apiService.refreshFileViewStates(props.id);
    
    if (prDetail.value) {
      // Merge the updated viewed states with existing files
      prDetail.value.files = prDetail.value.files.map(file => {
        const updatedFile = updatedFiles.find(f => f.path === file.path);
        if (updatedFile) {
          return {
            ...file,
            viewedState: updatedFile.viewedState,
            viewedAt: updatedFile.viewedAt
          };
        }
        return file;
      });
    }
  } catch (error) {
    console.error('Failed to refresh file viewed states:', error);
  } finally {
    refreshingViewStates.value = false;
  }
};

const isFileViewed = (filePath: string): boolean => {
  if (!prDetail.value) return false;
  const file = prDetail.value.files.find(f => f.path === filePath);
  return file?.viewedState === 'VIEWED' || file?.viewed === true || (prDetail.value.viewedFiles?.includes(filePath) ?? false);
};

const viewedFileSet = computed(() => {
  const files = prDetail.value?.files ?? [];
  const viewedPaths = new Set(prDetail.value?.viewedFiles ?? []);
  
  return new Set(
    files
      .filter(f => f.path && (f.viewedState === 'VIEWED' || f.viewed === true || viewedPaths.has(f.path)))
      .map(f => f.path!)
  );
});
const handleToggleViewed = async (filePath: string, viewed: boolean) => {
  if (!prDetail.value) return;

  if (!prDetail.value.viewedFiles) {
    prDetail.value.viewedFiles = [];
  }

  if (viewed) {
    if (!prDetail.value.viewedFiles.includes(filePath)) {
      prDetail.value.viewedFiles.push(filePath);
    }
  } else {
    prDetail.value.viewedFiles = prDetail.value.viewedFiles.filter(f => f !== filePath);
  }

  // Update file object with viewedState
  const fileIndex = prDetail.value.files.findIndex(f => f.path === filePath);
  if (fileIndex !== -1) {
    prDetail.value.files[fileIndex] = {
      ...prDetail.value.files[fileIndex],
      viewed,
      viewedState: viewed ? 'VIEWED' : 'UNVIEWED',
      viewedAt: viewed ? new Date().toISOString() : null
    };
  }

  // Save to preferences and sync with GitHub
  await saveViewedFiles(filePath, viewed);
};

const checksStatusLabel = computed(() => {
  if (!prDetail.value?.checksStatus) return 'No checks';
  switch (prDetail.value.checksStatus) {
    case 'SUCCESS':
      return 'All checks passed';
    case 'FAILURE':
      return 'Some checks failed';
    case 'PENDING':
      return 'Checks pending';
    default:
      return 'Checks running';
  }
});

const startEditingTitle = () => {
  if (prDetail.value?.isMerged) return;
  editTitleValue.value = prDetail.value?.title || '';
  isEditingTitle.value = true;
  nextTick(() => {
    titleInputRef.value?.focus();
    titleInputRef.value?.select();
  });
};

const saveTitle = async () => {
  if (!editTitleValue.value.trim() || editTitleValue.value === prDetail.value?.title) {
    cancelEditingTitle();
    return;
  }
  
  savingTitle.value = true;
  const result = await updatePR(props.id, { title: editTitleValue.value.trim() });
  savingTitle.value = false;
  
  if (result.success) {
    isEditingTitle.value = false;
  }
};

const cancelEditingTitle = () => {
  isEditingTitle.value = false;
  editTitleValue.value = '';
};

const handleTitleKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Enter') {
    event.preventDefault();
    saveTitle();
  }
  if (event.key === 'Escape') {
    cancelEditingTitle();
  }
};

const handleDescriptionSave = async (body: string) => {
  await updatePR(props.id, { body });
};

const handleEditComment = async (commentId: number, body: string): Promise<void> => {
  await editComment(commentId, body);
};

const handleDeleteComment = async (commentId: number): Promise<void> => {
  await deleteComment(commentId);
};

const getCheckStatusFromCheckRun = (check: CheckRun): 'SUCCESS' | 'FAILURE' | 'PENDING' | 'NEUTRAL' => {
  if (check.status === 'completed') {
    if (check.conclusion === 'success') return 'SUCCESS';
    if (check.conclusion === 'failure') return 'FAILURE';
    return 'NEUTRAL';
  }
  if (check.status === 'queued' || check.status === 'in_progress') return 'PENDING';
  return 'NEUTRAL';
};

</script>
