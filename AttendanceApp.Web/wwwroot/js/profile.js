/**
 * @fileoverview Profile page functionality.
 * Handles followers/following modal, follow/unfollow toggle, and logout.
 */

'use strict';

(function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Number of users to load per page */
  const PAGE_SIZE = 15;

  /** User list modes */
  const ListMode = Object.freeze({
    FOLLOWERS: 'followers',
    FOLLOWING: 'following'
  });

  /** API endpoints */
  const Endpoints = Object.freeze({
    FOLLOWERS: (userId) => `/api/userFollowings/followers/${userId}`,
    FOLLOWING: (userId) => `/api/userFollowings/following/${userId}`,
    TOGGLE_FOLLOW: (userId) => `/api/users/toggleFollow/${userId}`
  });

  /** Authentication */
  const Auth = Object.freeze({
    COOKIE_NAME: 'AttendanceApp.Jwt',
    LOGIN_URL: '/auth/login'
  });

  // ============================================================================
  // State
  // ============================================================================

  const state = {
    currentMode: ListMode.FOLLOWERS,
    currentPage: 0,
    totalCount: 0,
    loadedUsers: [],
    isLoading: false
  };

  // ============================================================================
  // DOM Elements
  // ============================================================================

  const elements = {
    modal: document.getElementById('usersModal'),
    modalTitle: document.getElementById('usersModalTitle'),
    modalBody: document.getElementById('usersModalBody'),
    followersBtn: document.getElementById('followersBtn'),
    followingBtn: document.getElementById('followingBtn'),
    followToggleBtn: document.getElementById('followToggleBtn'),
    logoutBtn: document.getElementById('logoutBtn')
  };

  // Early exit if required elements are missing
  if (!elements.modal || !elements.modalBody) {
    return;
  }

  // ============================================================================
  // Utility Functions
  // ============================================================================

  /**
   * Escapes HTML special characters to prevent XSS.
   * @param {string} str - The string to escape.
   * @returns {string} The escaped string.
   */
  function escapeHtml(str) {
    return String(str ?? '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  /**
   * Generates initials from a name.
   * @param {string} name - The full name.
   * @returns {string} The initials (1-2 characters).
   */
  function getInitials(name) {
    const parts = (name || '').trim().split(/\s+/);

    if (parts.length >= 2) {
      const firstInitial = parts[0][0] || '';
      const lastInitial = parts[parts.length - 1][0] || '';
      return (firstInitial + lastInitial).toUpperCase();
    }

    return (name || '?')[0].toUpperCase();
  }

  /**
   * Gets the user ID from the global profile data.
   * @returns {string|null} The user ID or null if not found.
   */
  function getUserId() {
    return globalThis.profileData?.userId || null;
  }

  // ============================================================================
  // Modal Functions
  // ============================================================================

  /**
   * Opens the users modal.
   */
  function openModal() {
    elements.modal.classList.add('open');
    elements.modal.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
  }

  /**
   * Closes the users modal.
   */
  function closeModal() {
    elements.modal.classList.remove('open');
    elements.modal.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
  }

  // ============================================================================
  // Rendering Functions
  // ============================================================================

  /**
   * Renders a single user list item.
   * @param {Object} user - The user object with id, name, and email.
   * @returns {string} The HTML string for the user item.
   */
  function renderUserItem(user) {
    const initials = getInitials(user.name);
    const escapedId = escapeHtml(user.id);
    const escapedInitials = escapeHtml(initials);
    const escapedName = escapeHtml(user.name);
    const escapedEmail = escapeHtml(user.email);

    return `
      <a href="/profile/view/${escapedId}" class="user-list-item">
        <div class="user-list-avatar">${escapedInitials}</div>
        <div class="user-list-info">
          <div class="user-list-name">${escapedName}</div>
          <div class="user-list-email">${escapedEmail}</div>
        </div>
      </a>
    `;
  }

  /**
   * Renders the load more button.
   * @returns {string} The HTML string for the load more button.
   */
  function renderLoadMoreButton() {
    return `
      <div class="users-load-more" id="loadMoreBtn">
        <span class="plus-icon">+</span>
        <span>Load more</span>
      </div>
    `;
  }

  /**
   * Renders the users list in the modal.
   */
  function renderUsers() {
    if (state.loadedUsers.length === 0 && !state.isLoading) {
      elements.modalBody.innerHTML = `<div class="users-empty">No ${state.currentMode} yet</div>`;
      return;
    }

    const userItemsHtml = state.loadedUsers.map(renderUserItem).join('');
    const hasMore = state.loadedUsers.length < state.totalCount;
    const loadMoreHtml = hasMore ? renderLoadMoreButton() : '';

    elements.modalBody.innerHTML = userItemsHtml + loadMoreHtml;

    // Attach load more click handler
    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (loadMoreBtn) {
      loadMoreBtn.addEventListener('click', loadMore);
    }
  }

  /**
   * Shows a loading indicator in the modal.
   */
  function showLoading() {
    if (state.loadedUsers.length === 0) {
      elements.modalBody.innerHTML = '<div class="users-loading">Loading...</div>';
    }
  }

  /**
   * Shows an error message in the modal.
   */
  function showError() {
    elements.modalBody.innerHTML = '<div class="users-empty">Failed to load</div>';
  }

  // ============================================================================
  // API Functions
  // ============================================================================

  /**
   * Fetches users from the API.
   * @param {string} mode - The list mode (followers or following).
   * @param {number} page - The page number to fetch.
   */
  async function fetchUsers(mode, page) {
    if (state.isLoading) {
      return;
    }

    state.isLoading = true;

    if (page === 0) {
      showLoading();
    }

    const userId = getUserId();
    if (!userId) {
      console.error('[Profile] No userId found');
      state.isLoading = false;
      return;
    }

    try {
      const endpoint = mode === ListMode.FOLLOWERS
        ? Endpoints.FOLLOWERS(userId)
        : Endpoints.FOLLOWING(userId);

      const params = new URLSearchParams({
        pageIndex: page.toString(),
        pageSize: PAGE_SIZE.toString()
      });

      const response = await fetch(`${endpoint}?${params}`, {
        method: 'GET',
        credentials: 'include'
      });

      if (!response.ok) {
        console.error('[Profile] Fetch failed:', response.status);
        showError();
        return;
      }

      const data = await response.json();
      state.totalCount = data.total || 0;

      if (page === 0) {
        state.loadedUsers = data.items || [];
      } else {
        state.loadedUsers = state.loadedUsers.concat(data.items || []);
      }

      state.currentPage = page;
      renderUsers();
    } catch (error) {
      console.error('[Profile] Fetch error:', error);
      showError();
    } finally {
      state.isLoading = false;
    }
  }

  /**
   * Loads the next page of users.
   */
  function loadMore() {
    if (state.isLoading || state.loadedUsers.length >= state.totalCount) {
      return;
    }

    fetchUsers(state.currentMode, state.currentPage + 1);
  }

  /**
   * Opens the modal and shows users for the specified mode.
   * @param {string} mode - The list mode (followers or following).
   */
  function showUsers(mode) {
    // Reset state
    state.currentMode = mode;
    state.currentPage = 0;
    state.totalCount = 0;
    state.loadedUsers = [];

    // Update modal title
    const title = mode === ListMode.FOLLOWERS ? 'Followers' : 'Following';
    elements.modalTitle.textContent = title;

    // Open and fetch
    openModal();
    fetchUsers(mode, 0);
  }

  // ============================================================================
  // Follow Toggle Functions
  // ============================================================================

  /**
   * Updates the follow button UI state.
   * @param {boolean} isFollowing - Whether the user is now following.
   */
  function updateFollowButtonState(isFollowing) {
    elements.followToggleBtn.dataset.isFollowing = isFollowing.toString();
    elements.followToggleBtn.textContent = isFollowing ? 'Following' : 'Follow';
    elements.followToggleBtn.classList.toggle('following', isFollowing);
  }

  /**
   * Updates the follower count display.
   * @param {number} delta - The change in follower count (+1 or -1).
   */
  function updateFollowerCount(delta) {
    const countEl = elements.followersBtn?.querySelector('.stat-count');
    if (countEl) {
      const current = Number.parseInt(countEl.textContent, 10) || 0;
      countEl.textContent = Math.max(0, current + delta);
    }
  }

  /**
   * Handles the follow/unfollow toggle action.
   */
  async function handleFollowToggle() {
    const userId = elements.followToggleBtn.dataset.userId;
    const isCurrentlyFollowing = elements.followToggleBtn.dataset.isFollowing === 'true';

    elements.followToggleBtn.disabled = true;

    try {
      const response = await fetch(Endpoints.TOGGLE_FOLLOW(userId), {
        method: 'POST',
        credentials: 'include'
      });

      if (response.ok) {
        const isNowFollowing = !isCurrentlyFollowing;
        updateFollowButtonState(isNowFollowing);
        updateFollowerCount(isNowFollowing ? 1 : -1);
      }
    } catch (error) {
      console.error('[Profile] Toggle follow error:', error);
    } finally {
      elements.followToggleBtn.disabled = false;
    }
  }

  // ============================================================================
  // Authentication Functions
  // ============================================================================

  /**
   * Handles the logout action.
   */
  function handleLogout() {
    // Clear the JWT cookie by setting it to expired
    document.cookie = `${Auth.COOKIE_NAME}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
    // Redirect to login
    globalThis.location.href = Auth.LOGIN_URL;
  }

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes all event listeners.
   */
  function initEventListeners() {
    // Followers button
    if (elements.followersBtn) {
      elements.followersBtn.addEventListener('click', function () {
        showUsers(ListMode.FOLLOWERS);
      });
    }

    // Following button
    if (elements.followingBtn) {
      elements.followingBtn.addEventListener('click', function () {
        showUsers(ListMode.FOLLOWING);
      });
    }

    // Modal close buttons
    elements.modal.querySelectorAll('[data-close-modal]').forEach(function (el) {
      el.addEventListener('click', closeModal);
    });

    // Close on Escape key
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && elements.modal.classList.contains('open')) {
        closeModal();
      }
    });

    // Follow toggle button
    if (elements.followToggleBtn) {
      elements.followToggleBtn.addEventListener('click', handleFollowToggle);
    }

    // Logout button
    if (elements.logoutBtn) {
      elements.logoutBtn.addEventListener('click', handleLogout);
    }
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  initEventListeners();
})();
