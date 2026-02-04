<template>
  <div class="flex flex-col bg-gradient-to-br from-slate-950 via-slate-950 to-slate-900 text-slate-200">
    <!-- Compact Header -->
    <div class="sticky top-20 z-20 bg-gradient-to-r from-slate-900/95 to-slate-900/90 border-b border-slate-700/30 backdrop-blur-md">
      <div class="px-5 py-3 flex items-center gap-3">
        <Breadcrumb
          v-if="prDetail"
          :items="[
            { label: 'Dashboard', to: '/' },
            { label: prDetail.repository },
            { label: `#${prDetail.gitHubId}` },
          ]"
        />
        <div v-else class="text-slate-500 text-sm">Loading...</div>

        <div class="flex-1 min-w-0 flex items-center gap-3">
          <StatusBadge v-if="prDetail" :status="prDetail.status" />
          <h1 class="text-lg font-semibold text-slate-100 truncate tracking-tight">
            {{ prDetail?.title || 'Loading...' }}
          </h1>
        </div>

        <div class="flex items-center gap-3 text-xs">
          <span class="text-slate-400 font-medium">{{ prDetail?.repository }} #{{ prDetail?.gitHubId }}</span>

          <button
            @click="showReviewModal = true"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-lg text-xs font-medium text-white transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30"
          >
            Review
          </button>

          <button
            @click="toggleCommentsPanel"
            :class="[
              'p-2 rounded-lg relative transition-all duration-200',
              commentsPanel 
                ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20' 
                : 'bg-slate-800/60 hover:bg-slate-700/60 text-slate-300 border border-slate-600/50 hover:border-slate-500/50'
            ]"
            title="Comments"
          >
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10c0 3.866-3.582 7-8 7a8.841 8.841 0 01-4.083-.98L2 17l1.338-3.123C2.493 12.767 2 11.434 2 10c0-3.866 3.582-7 8-7s8 3.134 8 7zM7 9H5v2h2V9zm8 0h-2v2h2V9zM9 9h2v2H9V9z" clip-rule="evenodd" />
            </svg>
            <span
              v-if="prDetail?.allComments && prDetail.allComments.length > 0"
              class="absolute -top-1.5 -right-1.5 bg-blue-500 text-white text-[10px] font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-0.5 shadow-lg"
            >
              {{ prDetail.allComments.length }}
            </span>
          </button>

          <button
            @click="refreshFileViewStates"
            :disabled="refreshingViewStates"
            class="px-3 py-2 bg-slate-800/60 hover:bg-slate-700/60 border border-slate-600/50 rounded-lg text-xs text-slate-200 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 hover:border-slate-500/50"
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
            class="px-3 py-2 bg-slate-800/60 hover:bg-slate-700/60 border border-slate-600/50 rounded-lg text-xs text-slate-200 transition-all duration-200 hover:border-slate-500/50"
          >
            GitHub
          </a>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center">
        <svg class="animate-spin h-16 w-16 mx-auto text-blue-500 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-slate-400 text-sm font-medium">Loading PR details...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center flex-1 min-h-[400px]">
      <div class="text-center max-w-md">
        <svg class="w-20 h-20 mx-auto text-red-500 mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" 
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h2 class="text-2xl font-semibold text-slate-200 mb-3">Failed to load PR</h2>
        <p class="text-slate-400 mb-6 text-sm">{{ error }}</p>
        <button
          @click="fetchPRDetail(props.id)"
          class="px-5 py-2.5 bg-blue-600 hover:bg-blue-500 rounded-lg text-white transition-all duration-200 shadow-lg shadow-blue-500/20 hover:shadow-blue-500/30 font-medium"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- Main Content - Full Page Layout -->
    <div v-else-if="prDetail" class="flex flex-1">
      <!-- File Tree Sidebar (Resizable) -->
      <div
        v-if="preferences.fileTreeVisible"
        :style="{ width: `${fileTreeWidth}px`, height: '100vh' }"
        class="flex-shrink-0 overflow-auto bg-gradient-to-b from-slate-900/40 to-slate-950/40 border-r border-slate-700/30 relative sticky top-0 backdrop-blur-sm"
      >
        <FileTree
          :files="prDetail.files"
          :selected-file="selectedFile || undefined"
          @select-file="scrollToFile"
        />
        <!-- Resize Handle -->
        <div
          class="absolute top-0 right-0 w-1.5 h-full cursor-ew-resize hover:bg-blue-500/50 bg-slate-600/20 hover:bg-blue-500/30 transition-all duration-200"
          @mousedown="startResizeFileTree"
        />
      </div>

      <!-- Main Content Area -->
      <div class="flex-1 flex flex-col min-w-0">
        <!-- Top Section: PR Info -->
        <div class="border-b border-slate-700/30 bg-gradient-to-b from-slate-900/30 to-transparent">
          <div class="flex gap-6 p-6">
            <!-- Left: Description (Largest) -->
            <div class="flex-1 min-w-0">
              <!-- Branch Info -->
              <div class="p-4 bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/30 rounded-xl mb-4 shadow-sm">
                <div class="text-xs font-medium text-slate-400 mb-2">Branches</div>
                <div class="flex items-center gap-2 text-sm">
                  <span class="text-emerald-400 font-mono">{{ prDetail.sourceBranch }}</span>
                  <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                  <span class="text-blue-400 font-mono">{{ prDetail.targetBranch }}</span>
                </div>
              </div>

              <!-- Description -->
              <div class="p-4 bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/30 rounded-xl shadow-sm">
                <div class="text-xs font-semibold text-slate-200 mb-3">Description</div>
                <DescriptionRenderer
                  v-if="prDetail"
                  :content="prDetail?.description || ''"
                />
                <p v-else class="text-sm text-slate-500 italic">No description provided</p>
              </div>
            </div>

            <!-- Right: Info & Stats -->
            <div class="w-96 flex-shrink-0 space-y-4">
              <!-- Stats - Compact Table -->
              <div class="p-4 bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/30 rounded-xl shadow-sm">
                <div class="text-xs font-medium text-slate-400 mb-3">Stats</div>
                <table class="w-full text-sm">
                  <tr class="border-b border-slate-700/20 last:border-0">
                    <td class="text-slate-500 py-2">Files</td>
                    <td class="text-slate-200 text-right font-semibold">{{ prDetail.changedFiles }}</td>
                  </tr>
                  <tr class="border-b border-slate-700/20 last:border-0">
                    <td class="text-slate-500 py-2">Comments</td>
                    <td class="text-blue-400 text-right font-semibold">{{ prDetail.allComments.length }}</td>
                  </tr>
                  <tr class="border-b border-slate-700/20 last:border-0">
                    <td class="text-slate-500 py-2">Additions</td>
                    <td class="text-emerald-400 text-right font-semibold">+{{ prDetail.additions }}</td>
                  </tr>
                  <tr>
                    <td class="text-slate-500 py-2">Deletions</td>
                    <td class="text-rose-400 text-right font-semibold">-{{ prDetail.deletions }}</td>
                  </tr>
                </table>
              </div>

                <!-- Reviewers - Compact with Icons -->
                <div v-if="prDetail.reviews.length > 0" class="p-4 bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/30 rounded-xl shadow-sm">
                  <div class="text-xs font-medium text-slate-400 mb-3">Reviewers</div>
                  <div class="space-y-2">
                    <div
                      v-for="review in prDetail.reviews"
                      :key="review.id"
                      class="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-700/30 transition-colors duration-200"
                    >
                      <img
                        v-if="review.reviewerAvatar"
                        :src="review.reviewerAvatar"
                        :alt="review.reviewer"
                        class="w-7 h-7 rounded-full ring-2 ring-slate-600/50"
                      />
                      <span class="text-sm text-slate-200 font-medium flex-1 truncate">{{ review.reviewer }}</span>
                      <component
                        v-if="getReviewStatusIcon(review.state)"
                        :is="getReviewStatusIcon(review.state)"
                        :class="['w-5 h-5', getReviewStatusColor(review.state)]"
                        :title="getReviewStatusLabel(review.state)"
                      />
                    </div>
                  </div>
                </div>
            </div>
          </div>
        </div>

        <!-- Bottom Section: File Diffs -->
        <div class="flex-1">
          <div class="px-6 py-5">
            <div class="space-y-4">
                <!-- Files Changed Header -->
               <div class="flex items-center justify-between py-2">
                  <h2 class="text-base font-semibold text-slate-100">
                    Files Changed <span class="text-slate-500 font-normal">({{ prDetail.files.length }})</span>
                  </h2>
                  <div class="flex items-center gap-2">
                    <button
                      @click="toggleFileTree"
                      class="flex items-center gap-2 px-3 py-2 bg-slate-800/50 border border-slate-700/50 rounded-lg hover:bg-slate-700/60 text-slate-300 text-xs transition-all duration-200 hover:border-slate-500/50"
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
                        class="p-2 bg-slate-800/50 border border-slate-700/50 rounded-lg hover:bg-slate-700/60 text-slate-300 transition-all duration-200 hover:border-slate-500/50"
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
                          class="absolute right-0 top-full mt-2 bg-slate-900/95 backdrop-blur-xl border border-slate-700/50 rounded-xl shadow-2xl z-50 min-w-[220px] p-2"
                        >
                          <div class="space-y-1">
                            <div class="text-xs font-semibold text-slate-400 px-3 py-2">Diff View</div>
                            <button
                              @click="setDiffViewMode('unified'); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs text-left transition-all duration-200',
                                preferences.diffViewMode === 'unified'
                                  ? 'bg-blue-600/20 text-blue-300 border border-blue-600/50'
                                  : 'text-slate-300 hover:bg-slate-800/60'
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
                                'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs text-left transition-all duration-200',
                                preferences.diffViewMode === 'split'
                                  ? 'bg-blue-600/20 text-blue-300 border border-blue-600/50'
                                  : 'text-slate-300 hover:bg-slate-800/60'
                              ]"
                            >
                              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 4v16M4 4h16" />
                              </svg>
                              Split
                            </button>
                            <div class="border-t border-slate-700/50 my-1"></div>
                            <div class="text-xs font-semibold text-slate-400 px-3 py-2">Context</div>
                            <button
                              @click="setShowContext(true); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs text-left transition-all duration-200',
                                preferences.showContext
                                  ? 'bg-blue-600/20 text-blue-300 border border-blue-600/50'
                                  : 'text-slate-300 hover:bg-slate-800/60'
                              ]"
                            >
                              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                              </svg>
                              Show Context
                            </button>
                            <button
                              @click="setShowContext(false); showSettingsDropdown = false"
                              :class="[
                                'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs text-left transition-all duration-200',
                                !preferences.showContext
                                  ? 'bg-blue-600/20 text-blue-300 border border-blue-600/50'
                                  : 'text-slate-300 hover:bg-slate-800/60'
                              ]"
                            >
                              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                              </svg>
                              Hide Context
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
                  :on-add-comment="(line: number, body: string) => handleAddComment(file.path!, line, body)"
                  :on-toggle-viewed="handleToggleViewed"
                  :initial-expanded="!isFileViewed(file.path!)"
                />
            </div>
          </div>
        </div>
      </div>

      <!-- Comments Panel (Resizable) -->
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
          :style="{ width: `${commentsPanelWidth}px` }"
          class="flex-shrink-0 relative"
        >
          <CommentsPanel
            :comments="prDetail?.allComments || []"
            :review-threads="prDetail?.reviewThreads || []"
            @close="toggleCommentsPanel"
            @scroll-to-comment="scrollToComment"
            @scroll-to-thread="scrollToThread"
          />
          <!-- Resize Handle -->
          <div
            class="absolute top-0 left-0 w-1.5 h-full cursor-ew-resize hover:bg-blue-500/50 bg-slate-600/20 hover:bg-blue-500/30 transition-all duration-200"
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
        class="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showReviewModal = false"
      >
        <div class="bg-gradient-to-br from-slate-900 to-slate-950 border border-slate-700/40 rounded-2xl p-6 max-w-lg w-full shadow-2xl shadow-black/50">
          <h3 class="text-lg font-semibold text-slate-100 mb-4">Review PR</h3>

          <div class="grid grid-cols-3 gap-2 mb-4">
            <button
              @click="reviewAction = 'APPROVED'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs font-medium transition-all duration-200 border',
                reviewAction === 'APPROVED'
                  ? 'bg-emerald-600 border-emerald-500 text-white shadow-lg shadow-emerald-500/20'
                  : 'bg-slate-800/50 border-slate-700/50 hover:bg-slate-800 text-slate-300 hover:border-slate-600/50'
              ]"
            >
              Approve
            </button>
            <button
              @click="reviewAction = 'CHANGES_REQUESTED'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs font-medium transition-all duration-200 border',
                reviewAction === 'CHANGES_REQUESTED'
                  ? 'bg-rose-600 border-rose-500 text-white shadow-lg shadow-rose-500/20'
                  : 'bg-slate-800/50 border-slate-700/50 hover:bg-slate-800 text-slate-300 hover:border-slate-600/50'
              ]"
            >
              Changes
            </button>
            <button
              @click="reviewAction = 'COMMENT'"
              :class="[
                'px-3 py-2.5 rounded-lg text-xs font-medium transition-all duration-200 border',
                reviewAction === 'COMMENT'
                  ? 'bg-blue-600 border-blue-500 text-white shadow-lg shadow-blue-500/20'
                  : 'bg-slate-800/50 border-slate-700/50 hover:bg-slate-800 text-slate-300 hover:border-slate-600/50'
              ]"
            >
              Comment
            </button>
          </div>

          <textarea
            v-model="reviewComment"
            :placeholder="reviewAction === 'APPROVED' ? 'Add your approval comment (optional)...' : reviewAction === 'CHANGES_REQUESTED' ? 'Describe changes requested... (required)' : 'Add your comment (optional)...'"
            :class="[
              'w-full px-4 py-3 border rounded-xl text-slate-200 text-sm resize-none focus:outline-none focus:ring-2 mb-4 transition-all duration-200 placeholder:text-slate-500',
              reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim()
                ? 'bg-slate-800/50 border-rose-500 focus:ring-rose-500/50 focus:border-rose-500'
                : 'bg-slate-800/50 border-slate-700/50 focus:ring-blue-500/50 focus:border-blue-500'
            ]"
            rows="4"
          />
          <div v-if="reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim()" class="text-rose-400 text-xs mb-4">
            Please provide details about the changes you're requesting.
          </div>
          <div class="flex gap-3 justify-end">
            <button
              @click="showReviewModal = false"
              class="px-4 py-2 bg-slate-700/50 hover:bg-slate-700 rounded-lg text-sm text-slate-200 transition-all duration-200 border border-slate-600/50 hover:border-slate-500/50"
            >
              Cancel
            </button>
            <button
              @click="handleSubmitReview"
              :disabled="submittingReview || (reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim())"
              :class="[
                'px-5 py-2 rounded-lg text-sm text-white transition-all duration-200 font-medium shadow-lg',
                reviewAction === 'APPROVED' ? 'bg-emerald-600 hover:bg-emerald-500 shadow-emerald-500/20 hover:shadow-emerald-500/30' :
                reviewAction === 'CHANGES_REQUESTED' ? 'bg-rose-600 hover:bg-rose-500 shadow-rose-500/20 hover:shadow-rose-500/30' :
                'bg-blue-600 hover:bg-blue-500 shadow-blue-500/20 hover:shadow-blue-500/30',
                { 'opacity-50 cursor-not-allowed shadow-none': submittingReview || (reviewAction === 'CHANGES_REQUESTED' && !reviewComment.trim()) }
              ]"
            >
              {{ submittingReview ? 'Submitting...' : 'Submit' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { usePRDetail } from '../composables/usePRDetail';
import { useUserPreferences } from '../composables/useUserPreferences';
import { apiService } from '../services/api';
import FileDiffViewer from '../components/FileDiffViewer.vue';
import FileTree from '../components/FileTree.vue';
import CommentsPanel from '../components/CommentsPanel.vue';
import StatusBadge from '../components/StatusBadge.vue';
import Breadcrumb from '../components/Breadcrumb.vue';
import DescriptionRenderer from '../components/DescriptionRenderer.vue';
import type { Comment } from '../types';
import { CheckIcon, ChatBubbleLeftIcon, ArrowPathIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';

const authStore = useAuthStore();

const props = defineProps<{
  id: number;
}>();

const {
  prDetail,
  loading,
  error,
  commentsPanel,
  fetchPRDetail,
  addComment,
  submitReview,
  toggleCommentsPanel,
  toggleFileTree,
} = usePRDetail();

const { preferences, loadPreferences, setFileTreeWidth, setCommentsPanelWidth, updatePreferences, setDiffViewMode, setShowContext } = useUserPreferences();

const selectedFile = ref<string | null>(null);
const showReviewModal = ref(false);
const showSettingsDropdown = ref(false);
const reviewAction = ref<'APPROVED' | 'CHANGES_REQUESTED' | 'COMMENT'>('COMMENT');
const reviewComment = ref('');
const submittingReview = ref(false);
const fileRefs = ref<Map<string, any>>(new Map());
const refreshingViewStates = ref(false);

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

  // Fetch user's file viewed states from GitHub
  await refreshFileViewStates();

  // Load viewed files from preferences
  if (preferences.value.viewedFilesByPr && preferences.value.viewedFilesByPr[props.id]) {
    if (prDetail.value) {
      prDetail.value.viewedFiles = preferences.value.viewedFilesByPr[props.id];
    }
  }
  
  document.addEventListener('keydown', handleKeyPress);
  document.addEventListener('mousemove', handleMouseMove);
  document.addEventListener('mouseup', handleMouseUp);
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyPress);
  document.removeEventListener('mousemove', handleMouseMove);
  document.removeEventListener('mouseup', handleMouseUp);
  document.removeEventListener('click', handleClickOutside);
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

const handleKeyPress = (e: KeyboardEvent) => {
  if ((e.target as HTMLElement).tagName === 'TEXTAREA' || (e.target as HTMLElement).tagName === 'INPUT') {
    return;
  }

  const key = e.key.toLowerCase();
  const shortcuts = preferences.value.keyboardShortcuts || {
    toggleComments: 'c',
    toggleFileTree: 'f',
    nextFile: 'j',
    previousFile: 'k',
    nextComment: 'n',
    previousComment: 'p',
  };

  if (key === shortcuts.toggleComments) {
    toggleCommentsPanel();
  } else if (key === shortcuts.toggleFileTree) {
    toggleFileTree();
  } else if (key === shortcuts.nextFile) {
    navigateFile('next');
  } else if (key === shortcuts.previousFile) {
    navigateFile('prev');
  } else if (key === shortcuts.nextComment) {
    navigateComment('next');
  } else if (key === shortcuts.previousComment) {
    navigateComment('prev');
  }
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (showSettingsDropdown.value && !target.closest('.relative')) {
    showSettingsDropdown.value = false;
  }
};

const handleAddComment = async (path: string, line: number, body: string) => {
  const comment = await addComment(props.id, { path, line, body });
  if (comment) {
    console.log('Comment added successfully');
  }
};

const handleSubmitReview = async () => {
  if (reviewAction.value === 'CHANGES_REQUESTED' && !reviewComment.value.trim()) {
    alert('Please provide details about the changes you are requesting.');
    return;
  }

  submittingReview.value = true;
  const success = await submitReview(props.id, {
    state: reviewAction.value,
    body: reviewComment.value || undefined,
  });
  submittingReview.value = false;
  
  if (success) {
    showReviewModal.value = false;
    reviewComment.value = '';
  }
};

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

const scrollToThread = (threadId: string, line?: number) => {
  const thread = prDetail.value?.reviewThreads.find(rt => rt.gitHubId === threadId);
  if (thread?.path) {
    scrollToFile(thread.path, line ?? undefined);
  }
};

const setFileRef = (path: string, el: any) => {
  if (el) {
    fileRefs.value.set(path, el);
  }
};

const navigateFile = (direction: 'next' | 'prev') => {
  if (!prDetail.value) return;
  
  const currentIndex = selectedFile.value 
    ? prDetail.value.files.findIndex(f => f.path === selectedFile.value)
    : -1;
  
  const nextIndex = direction === 'next' 
    ? (currentIndex + 1) % prDetail.value.files.length
    : (currentIndex - 1 + prDetail.value.files.length) % prDetail.value.files.length;
  
  const nextFile = prDetail.value.files[nextIndex];
  if (nextFile?.path) {
    scrollToFile(nextFile.path);
  }
};

const navigateComment = (direction: 'next' | 'prev') => {
  if (!prDetail.value) return;

  const comments = prDetail.value.allComments.filter(c => c.line && c.path);
  if (comments.length === 0) return;

  const currentHash = window.location.hash.match(/#L(\d+)/);
  const currentLine = currentHash ? parseInt(currentHash[1] || '0') : 0;

  const currentIndex = comments.findIndex(c => c.line === currentLine);
  let nextComment: Comment | undefined;

  if (direction === 'next') {
    nextComment = currentIndex >= 0 && currentIndex < comments.length - 1
      ? comments[currentIndex + 1]
      : comments[0];
  } else {
    nextComment = currentIndex > 0
      ? comments[currentIndex - 1]
      : comments[comments.length - 1];
  }

  if (nextComment && nextComment.line && nextComment.path) {
    scrollToFile(nextComment.path, nextComment.line);
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

const getReviewStatusIcon = (state: string) => {
  if (state === 'Approved') return CheckIcon;
  if (state === 'Commented') return ChatBubbleLeftIcon;
  if (state === 'ChangesRequested') return ArrowPathIcon;
  return null;
};

const getReviewStatusColor = (state: string): string => {
  if (state === 'Approved') return 'text-emerald-500';
  if (state === 'Commented') return 'text-amber-500';
  if (state === 'ChangesRequested') return 'text-rose-500';
  return 'text-gray-500';
};

const getReviewStatusLabel = (state: string): string => {
  if (state === 'Approved') return 'Accepted';
  if (state === 'Commented') return 'Commented';
  if (state === 'ChangesRequested') return 'Requested Changes';
  return state;
};
</script>
