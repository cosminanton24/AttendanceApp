/**
 * @fileoverview Professor Home page functionality.
 * Handles lecture creation, active lecture management, and attendee tracking.
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Milliseconds per minute */
  const MS_PER_MINUTE = 60 * 1000;

  /** Default duration in minutes */
  const DEFAULT_DURATION_MINUTES = 60;

  /** Modal close delay after success (ms) */
  const MODAL_CLOSE_DELAY_MS = 1000;

  /** Calendar registration check interval (ms) */
  const CALENDAR_CHECK_INTERVAL_MS = 50;

  /** Maximum calendar registration checks */
  const CALENDAR_MAX_CHECKS = 200;

  /** Default page size for lecture loading */
  const DEFAULT_PAGE_SIZE = 200;

  /** Attendee page size */
  const ATTENDEE_PAGE_SIZE = 20;

  /** Quiz page size */
  const QUIZ_PAGE_SIZE = 20;

  /** Quiz progress update interval (ms) */
  const QUIZ_PROGRESS_UPDATE_INTERVAL_MS = 1000;

  /** Search debounce delay (ms) */
  const SEARCH_DEBOUNCE_MS = 1000;

  /** QR code size in pixels */
  const QR_CODE_SIZE = 256;

  /**
   * Lecture status enum values.
   * @enum {number}
   */
  const LectureStatus = Object.freeze({
    SCHEDULED: 0,
    IN_PROGRESS: 1,
    ENDED: 2,
    CANCELED: 3
  });

  // ============================================================================
  // DOM Elements - Modal
  // ============================================================================

  const modalEl = document.getElementById('makeLectureModal');
  const form = document.getElementById('makeLectureForm');
  const feedbackEl = document.getElementById('makeLectureFeedback');
  const openBtn = document.getElementById('openMakeLectureBtn');

  if (!modalEl || !form || !feedbackEl) {
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
   * Normalizes a status value to a comparable format.
   * @param {number|string|null} status - The status value.
   * @returns {number|string|null} The normalized status.
   */
  function normalizeStatus(status) {
    if (status == null) {
      return null;
    }
    if (typeof status === 'number') {
      return status;
    }
    return String(status).trim().toLowerCase();
  }

  /**
   * Checks if a status represents an active (in-progress) lecture.
   * @param {number|string} status - The status value.
   * @returns {boolean} True if active.
   */
  function isActiveLectureStatus(status) {
    const normalized = normalizeStatus(status);
    return normalized === LectureStatus.IN_PROGRESS ||
           normalized === 'inprogress' ||
           normalized === 'in progress';
  }

  /**
   * Converts a status value to a human-readable label.
   * @param {number|string} status - The status value.
   * @returns {string} The status label.
   */
  function statusToLabel(status) {
    const normalized = normalizeStatus(status);

    if (normalized === LectureStatus.SCHEDULED || normalized === 'scheduled') {
      return 'Scheduled';
    }
    if (normalized === LectureStatus.IN_PROGRESS || normalized === 'inprogress') {
      return 'In Progress';
    }
    if (normalized === LectureStatus.ENDED || normalized === 'ended') {
      return 'Ended';
    }
    if (normalized === LectureStatus.CANCELED || normalized === 'canceled') {
      return 'Canceled';
    }

    return status ?? 'Unknown';
  }

  /**
   * Gets the join URL for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {string} The join URL.
   */
  function getJoinUrl(lectureId) {
    try {
      return new URL(
        `/lecture/join/${encodeURIComponent(String(lectureId))}`,
        globalThis.location.origin
      ).toString();
    } catch {
      return `/lecture/join/${encodeURIComponent(String(lectureId))}`;
    }
  }

  // ============================================================================
  // Duration Parsing Functions
  // ============================================================================

  /**
   * Parses a duration input into TimeSpan format (HH:MM:SS).
   * Supports: ISO 8601 (PT1H30M), hh:mm, 1h30m, 90m, 90.
   * @param {string} input - The duration input.
   * @returns {string|null} The TimeSpan string or null if invalid.
   */
  function parseDurationToTimeSpan(input) {
    if (!input) {
      return null;
    }

    const trimmed = input.trim();

    // ISO 8601 duration (PT1H30M)
    if (/^P/i.test(trimmed)) {
      try {
        const upper = trimmed.toUpperCase();
        const hoursMatch = /(\d+)H/.exec(upper);
        const minsMatch = /(\d+)M/.exec(upper);
        const secsMatch = /(\d+)S/.exec(upper);
        const hours = Number(hoursMatch?.[1] ?? 0);
        const mins = Number(minsMatch?.[1] ?? 0);
        const secs = Number(secsMatch?.[1] ?? 0);
        return formatTimeSpan(hours, mins, secs);
      } catch {
        // Fall through to other formats
      }
    }

    // hh:mm format
    if (/^\d+:\d+$/.test(trimmed)) {
      const parts = trimmed.split(':').map(Number);
      return formatTimeSpan(parts[0], parts[1], 0);
    }

    // 1h30m or 1h format
    const hoursMinutesMatch = /^(\d+)h(?:(\d+)m)?$/i.exec(trimmed);
    if (hoursMinutesMatch) {
      const hours = Number(hoursMinutesMatch[1]);
      const mins = Number(hoursMinutesMatch[2] || 0);
      return formatTimeSpan(hours, mins, 0);
    }

    // Minutes only (90 or 90m)
    const minutesMatch = /^(\d+)(?:m)?$/i.exec(trimmed);
    if (minutesMatch) {
      const totalMin = Number(minutesMatch[1]);
      const hours = Math.floor(totalMin / 60);
      const mins = totalMin % 60;
      return formatTimeSpan(hours, mins, 0);
    }

    // Already hh:mm:ss format
    if (/^\d{1,2}:\d{2}:\d{2}$/.test(trimmed)) {
      return trimmed;
    }

    return null;
  }

  /**
   * Formats hours, minutes, seconds into TimeSpan string.
   * @param {number} hours - Hours.
   * @param {number} minutes - Minutes.
   * @param {number} seconds - Seconds.
   * @returns {string} The formatted TimeSpan.
   */
  function formatTimeSpan(hours, minutes, seconds) {
    const hh = String(hours).padStart(2, '0');
    const mm = String(minutes).padStart(2, '0');
    const ss = String(seconds).padStart(2, '0');
    return `${hh}:${mm}:${ss}`;
  }

  /**
   * Converts a TimeSpan or duration string to minutes.
   * @param {string|number|null} input - The duration input.
   * @returns {number} The duration in minutes.
   */
  function parseDurationToMinutes(input) {
    if (input == null) {
      return DEFAULT_DURATION_MINUTES;
    }

    if (typeof input === 'number') {
      return Math.max(0, Math.floor(input));
    }

    const str = String(input).trim();

    // ISO 8601 duration
    if (/^P/i.test(str)) {
      const upper = str.toUpperCase();
      const hoursMatch = /(\d+)H/.exec(upper);
      const minsMatch = /(\d+)M/.exec(upper);
      const secsMatch = /(\d+)S/.exec(upper);
      const hours = Number(hoursMatch?.[1] ?? 0);
      const mins = Number(minsMatch?.[1] ?? 0);
      const secs = Number(secsMatch?.[1] ?? 0);
      return hours * 60 + mins + Math.round(secs / 60);
    }

    // hh:mm:ss format
    const hhmmss = /^(\d{1,2}):(\d{2}):(\d{2})$/.exec(str);
    if (hhmmss) {
      return Number(hhmmss[1]) * 60 + Number(hhmmss[2]) + Math.round(Number(hhmmss[3]) / 60);
    }

    // hh:mm format
    const hhmm = /^(\d{1,2}):(\d{2})$/.exec(str);
    if (hhmm) {
      return Number(hhmm[1]) * 60 + Number(hhmm[2]);
    }

    // 1h30m format
    const hoursMinutes = /^(\d+)h(?:(\d+)m)?$/i.exec(str);
    if (hoursMinutes) {
      return Number(hoursMinutes[1]) * 60 + Number(hoursMinutes[2] || 0);
    }

    // Minutes only
    const mins = /^(\d+)(?:m)?$/i.exec(str);
    if (mins) {
      return Number(mins[1]);
    }

    return DEFAULT_DURATION_MINUTES;
  }

  /**
   * Converts a TimeSpan string to minutes (for form submission).
   * @param {string} timeSpan - The TimeSpan string.
   * @returns {number} The duration in minutes.
   */
  function timeSpanToMinutes(timeSpan) {
    if (!timeSpan) {
      return DEFAULT_DURATION_MINUTES;
    }

    const parts = String(timeSpan).split(':').map(Number);

    if (parts.length >= 2) {
      const hours = Number(parts[0] || 0);
      const mins = Number(parts[1] || 0);
      const secs = Number(parts[2] || 0);
      return hours * 60 + mins + Math.round(secs / 60);
    }

    const parsed = Number.parseInt(timeSpan, 10);
    return Number.isNaN(parsed) ? DEFAULT_DURATION_MINUTES : parsed;
  }

  // ============================================================================
  // Modal Functions
  // ============================================================================

  /**
   * Opens the create lecture modal.
   */
  function openModal() {
    modalEl.classList.add('open');
    modalEl.setAttribute('aria-hidden', 'false');
    showFeedback('', null);

    // Close calendar popover if present
    try {
      if (globalThis.AttendanceCalendar && typeof globalThis.AttendanceCalendar.close === 'function') {
        globalThis.AttendanceCalendar.close();
      }
    } catch {
      // Ignore calendar close errors
    }
  }

  /**
   * Closes the create lecture modal.
   */
  function closeModal() {
    modalEl.classList.remove('open');
    modalEl.setAttribute('aria-hidden', 'true');

    // Clear form and reset submit state
    try {
      form.querySelector('[name="title"]').value = '';
      form.querySelector('[name="date"]').value = '';
      form.querySelector('[name="start"]').value = '';
      form.querySelector('[name="duration"]').value = '';
      form.querySelector('[name="description"]').value = '';

      const submitBtn = form.querySelector('[type="submit"]');
      if (submitBtn) {
        submitBtn.disabled = false;
      }

      showFeedback('', null);
    } catch {
      // Ignore form reset errors
    }
  }

  /**
   * Displays feedback message in the modal.
   * @param {string} message - The message to display.
   * @param {string|null} type - The message type ('error', 'success', or null).
   */
  function showFeedback(message, type) {
    feedbackEl.textContent = message || '';
    feedbackEl.classList.remove('error', 'success');

    if (type === 'error') {
      feedbackEl.classList.add('error');
    } else if (type === 'success') {
      feedbackEl.classList.add('success');
    }
  }

  // ============================================================================
  // Form Submission
  // ============================================================================

  /**
   * Extracts an error message from an API response.
   * @param {Object} data - The response data.
   * @returns {string|null} The error message or null.
   */
  function extractErrorMessage(data) {
    if (!data) {
      return null;
    }

    // Prefer the API detail field
    if (data.detail) {
      // Try to extract message between '--' and 'Severity:' for validation errors
      const match = /--\s*(.*?)\s*Severity:/is.exec(String(data.detail));
      if (match?.[1]) {
        return match[1].trim();
      }
      // No match - use the full detail string
      return String(data.detail).trim();
    }

    if (data.message) {
      return data.message;
    }

    if (data.title) {
      return data.title;
    }

    if (data.errors) {
      return JSON.stringify(data.errors);
    }

    return null;
  }

  /**
   * Adds a newly created lecture to the calendar.
   * @param {string} createdId - The created lecture ID.
   * @param {string} title - The lecture title.
   * @param {string} description - The lecture description.
   * @param {string} startIso - The start time ISO string.
   * @param {number} minutes - The duration in minutes.
   */
  function addCreatedLectureToCalendar(createdId, title, description, startIso, minutes) {
    if (!globalThis.AttendanceCalendar || typeof globalThis.AttendanceCalendar.addEvent !== 'function') {
      return;
    }

    const startDate = startIso ? new Date(startIso) : null;
    const endDate = startDate ? new Date(startDate.getTime() + minutes * MS_PER_MINUTE) : null;

    if (!startDate || !endDate) {
      return;
    }

    try {
      globalThis.AttendanceCalendar.addEvent({
        id: createdId || null,
        title: title,
        description: description || '',
        start: startDate,
        end: endDate,
        status: LectureStatus.SCHEDULED
      });
    } catch (error) {
      console.error('[ProfessorHome] Failed to add created lecture to calendar:', error);
    }
  }

  /**
   * Handles the form submission for creating a lecture.
   * @param {Event} event - The submit event.
   */
  async function handleFormSubmit(event) {
    event.preventDefault();

    const title = form.querySelector('[name="title"]').value.trim();
    const date = form.querySelector('[name="date"]').value;
    const start = form.querySelector('[name="start"]').value;
    const duration = form.querySelector('[name="duration"]').value.trim();
    const description = form.querySelector('[name="description"]').value.trim();

    // Client-side validation
    if (!title || !date || !start || !duration || !description) {
      showFeedback('Please complete all fields.', 'error');
      return;
    }

    // Parse and validate date/time
    let startIso = null;
    try {
      const dateTime = new Date(date + 'T' + start);
      if (Number.isNaN(dateTime.getTime())) {
        throw new TypeError('Invalid date/time');
      }
      startIso = dateTime.toISOString();
    } catch {
      showFeedback('Invalid date or start time.', 'error');
      return;
    }

    // Parse and validate duration
    const durationTs = parseDurationToTimeSpan(duration);
    if (!durationTs) {
      showFeedback('Invalid duration format. See example formats.', 'error');
      return;
    }

    const payload = {
      name: title,
      description: description,
      startTime: startIso,
      duration: durationTs
    };

    const submitBtn = form.querySelector('[type="submit"]');
    if (submitBtn) {
      submitBtn.disabled = true;
    }

    try {
      const response = await fetch('/api/lectures', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        let message = `Error ${response.status}`;

        try {
          const data = await response.json();
          const extracted = extractErrorMessage(data);
          if (extracted) {
            message = extracted;
          }
        } catch {
          try {
            message = await response.text();
          } catch {
            // Use default message
          }
        }

        showFeedback(message, 'error');
        if (submitBtn) {
          submitBtn.disabled = false;
        }
        return;
      }

      // Success - add event to calendar
      showFeedback('Created', 'success');

      try {
        const created = await response.json();
        const createdId = (created && typeof created === 'string')
          ? created
          : (created?.Id || created);

        const minutes = timeSpanToMinutes(durationTs);
        addCreatedLectureToCalendar(createdId, title, description, startIso, minutes);
      } catch {
        // Ignore JSON parse errors
      }

      // Close after delay
      setTimeout(closeModal, MODAL_CLOSE_DELAY_MS);
    } catch (error) {
      console.error('[ProfessorHome] Form submit error:', error);
      showFeedback('Network error', 'error');

      if (submitBtn) {
        submitBtn.disabled = false;
      }
    }
  }

  // ============================================================================
  // Lecture Loading
  // ============================================================================

  /**
   * Loads professor lectures and adds them to the calendar.
   * @param {Object} options - Load options.
   * @param {number} [options.page=0] - The page number.
   * @param {number} [options.pageSize=200] - The page size.
   * @param {number|null} [options.fromMonthsAgo=null] - Filter by months ago.
   */
  async function loadProfessorLectures(options = {}) {
    const { page = 0, pageSize = DEFAULT_PAGE_SIZE, fromMonthsAgo = null } = options;

    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));

    if (fromMonthsAgo != null) {
      params.set('fromMonthsAgo', String(fromMonthsAgo));
    }

    try {
      const response = await fetch('/api/lectures/me?' + params.toString(), {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return;
      }

      const data = await response.json();

      if (!data || data.length === 0) {
        return;
      }

      // Render active lecture cards
      try {
        for (const item of data) {
          cacheLectureMeta(item);

          const id = item?.id ?? item?.Id;
          const status = item?.status ?? item?.Status;

          if (isActiveLectureStatus(status)) {
            upsertActiveLectureCard(item);
          } else if (id != null) {
            removeActiveLectureCard(id);
          }
        }

        // Fetch attendee totals once
        try {
          fetchActiveLectureAttendeeCounts();
        } catch {
          // Ignore fetch errors
        }
      } catch (error) {
        console.error('[ProfessorHome] Failed to render active lecture cards:', error);
      }

      // Add lectures to calendar
      if (globalThis.AttendanceCalendar && typeof globalThis.AttendanceCalendar.addEvent === 'function') {
        for (const item of data) {
          addLectureToCalendar(item);
        }
      }
    } catch (error) {
      console.error('[ProfessorHome] Load lectures error:', error);
    }
  }

  /**
   * Adds a single lecture item to the calendar.
   * @param {Object} item - The lecture item.
   */
  function addLectureToCalendar(item) {
    const name = item.name || item.Name || 'Untitled';
    const startTime = item.startTime || item.StartTime;
    const description = item.description || item.Description || '';
    const duration = item.duration || item.Duration || null;
    const eventId = item.id || item.Id || null;

    const minutes = parseDurationToMinutes(duration);

    try {
      const startDate = startTime ? new Date(startTime) : null;
      const endDate = startDate ? new Date(startDate.getTime() + (minutes * MS_PER_MINUTE)) : null;

      if (startDate && endDate) {
        globalThis.AttendanceCalendar.addEvent({
          id: eventId,
          title: name,
          description: description || '',
          start: startDate,
          end: endDate,
          status: item.status ?? item.Status
        });
      }
    } catch (error) {
      console.error('[ProfessorHome] Failed to add lecture to calendar:', error, item);
    }
  }

  // ============================================================================
  // Active Lecture Cards
  // ============================================================================

  /** Host element for active lecture cards */
  const activeLecturesHost = document.getElementById('professorActiveLectures');

  /** Map of lecture ID to card element */
  const activeLectureCardsById = new Map();

  /** Map of lecture ID to metadata (name, description) */
  const lectureMetaById = new Map();

  /**
   * Caches lecture metadata for later use.
   * @param {Object} item - The lecture item.
   */
  function cacheLectureMeta(item) {
    try {
      const id = item?.id ?? item?.Id;
      if (!id) {
        return;
      }

      const key = String(id);
      const name = item?.name ?? item?.Name;
      const description = item?.description ?? item?.Description;

      if (name != null || description != null) {
        const existing = lectureMetaById.get(key);
        lectureMetaById.set(key, {
          name: name ?? existing?.name,
          description: description ?? existing?.description
        });
      }
    } catch {
      // Ignore cache errors
    }
  }

  /**
   * Ensures a QR code is rendered in the element.
   * @param {HTMLElement} element - The container element.
   * @param {string} urlText - The URL to encode.
   */
  function ensureQr(element, urlText) {
    if (!element) {
      return;
    }

    try {
      element.innerHTML = '';
    } catch {
      // Ignore clear errors
    }

    // Prefer local QRCode generator if present
    if (globalThis.QRCode) {
      try {
        new globalThis.QRCode(element, {
          text: urlText,
          width: QR_CODE_SIZE,
          height: QR_CODE_SIZE,
          correctLevel: globalThis.QRCode.CorrectLevel ? globalThis.QRCode.CorrectLevel.M : undefined
        });
        return;
      } catch {
        // Fall through to image fallback
      }
    }

    // Fallback: public QR image generator
    try {
      const img = document.createElement('img');
      img.alt = 'QR code';
      img.loading = 'lazy';
      img.referrerPolicy = 'no-referrer';
      img.src = `https://api.qrserver.com/v1/create-qr-code/?size=${QR_CODE_SIZE}x${QR_CODE_SIZE}&data=${encodeURIComponent(urlText)}`;
      element.appendChild(img);
    } catch {
      // Ignore QR fallback errors
    }
  }

  /**
   * Creates or updates an active lecture card.
   * @param {Object} item - The lecture item.
   */
  function upsertActiveLectureCard(item) {
    if (!activeLecturesHost) {
      return;
    }

    const lectureId = item?.id ?? item?.Id;
    if (!lectureId) {
      return;
    }

    const key = String(lectureId);
    cacheLectureMeta(item);

    let card = activeLectureCardsById.get(key);

    if (!card) {
      card = document.createElement('div');
      card.className = 'professor-home professor-lecture-card';
      card.dataset.lectureId = key;
      card.innerHTML = `
        <h4 class="pl-title"></h4>
        <p class="pl-desc"></p>
        <div class="pl-qr-wrap"><div class="pl-qr"></div></div>
        <div class="pl-id"></div>
        <div class="pl-attendees">
          <div class="pl-att-count" aria-live="polite">Attendees: —</div>
          <button type="button" class="btn-inline pl-att-btn">Attendees</button>
        </div>
        <div class="pl-quiz-actions">
          <button type="button" class="btn-inline pl-quiz-btn">Start Quiz</button>
        </div>
        <div class="pl-quiz-progress" style="display:none;">
          <div class="pl-quiz-progress__head">
            <div class="pl-quiz-progress__left">
              <div class="pl-quiz-progress__title">Active Quiz</div>
              <div class="pl-quiz-progress__name"></div>
            </div>
            <div class="pl-quiz-progress__remaining"></div>
          </div>
          <div class="pl-quiz-progress__track">
            <div class="pl-quiz-progress__fill"></div>
            <div class="pl-quiz-progress__now"></div>
          </div>
          <div class="pl-quiz-progress__submissions">
            <span class="pl-quiz-progress__submissions-label">Submissions:</span>
            <span class="pl-quiz-progress__submissions-count">0</span>
          </div>
        </div>
      `;
      activeLecturesHost.appendChild(card);
      activeLectureCardsById.set(key, card);

      // Wire attendee popover button
      const btn = card.querySelector('.pl-att-btn');
      if (btn) {
        btn.addEventListener('click', function (event) {
          event.preventDefault();
          event.stopPropagation();
          openAttendeesPopover(key, btn);
        });
      }

      // Wire quiz button
      const quizBtn = card.querySelector('.pl-quiz-btn');
      if (quizBtn) {
        quizBtn.addEventListener('click', function (event) {
          event.preventDefault();
          event.stopPropagation();
          openQuizSelectionModal(key);
        });
      }

      // Fetch and display active quiz progress
      fetchAndDisplayActiveQuiz(key, card);
    }

    const cached = lectureMetaById.get(key);
    const title = item?.name ?? item?.Name ?? cached?.name ?? 'Untitled';
    const description = item?.description ?? item?.Description ?? cached?.description ?? '';
    const joinUrl = getJoinUrl(key);

    const titleEl = card.querySelector('.pl-title');
    const descEl = card.querySelector('.pl-desc');
    const qrEl = card.querySelector('.pl-qr');
    const idEl = card.querySelector('.pl-id');

    if (titleEl) {
      titleEl.textContent = String(title);
    }
    if (descEl) {
      descEl.textContent = String(description);
    }
    if (idEl) {
      idEl.textContent = `Lecture ID (join): ${key}`;
    }

    ensureQr(qrEl, joinUrl);
  }

  /**
   * Removes an active lecture card.
   * @param {string} lectureId - The lecture ID.
   */
  function removeActiveLectureCard(lectureId) {
    if (!lectureId) {
      return;
    }

    const key = String(lectureId);
    const card = activeLectureCardsById.get(key);

    if (card?.parentNode) {
      try {
        card.remove();
      } catch {
        // Ignore removal errors
      }
    }

    activeLectureCardsById.delete(key);

    // If popover is currently showing this lecture, close it
    try {
      if (attendeesPopoverState?.lectureId === key) {
        closeAttendeesPopover();
      }
    } catch {
      // Ignore popover close errors
    }
  }

  // ============================================================================
  // Attendee Management
  // ============================================================================

  /** Set of lecture IDs currently fetching attendee totals */
  const attendeeTotalsInFlight = new Set();

  /**
   * Fetches a page of attendees for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {number} page - The page number.
   * @param {number} pageSize - The page size.
   * @param {string|null} searchFilter - Optional search filter.
   * @returns {Promise<{items: Array, total: number}|null>} The page data or null.
   */
  async function fetchLectureAttendeesPage(lectureId, page, pageSize, searchFilter = null) {
    let baseUrl = `/api/lectureAttendees/${encodeURIComponent(String(lectureId))}?page=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`;

    if (searchFilter?.trim()) {
      baseUrl += `&searchFilter=${encodeURIComponent(searchFilter.trim())}`;
    }

    try {
      const response = await fetch(baseUrl, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return null;
      }

      const data = await response.json();
      const items = data?.items ?? data?.Items ?? [];
      const total = data?.total ?? data?.Total ?? 0;

      return {
        items: Array.isArray(items) ? items : [],
        total: Number(total || 0)
      };
    } catch {
      return null;
    }
  }

  /**
   * Fetches user info for a batch of user IDs.
   * @param {Array<string>} ids - The user IDs.
   * @returns {Promise<Map<string, {name: string, email: string}>>} Map of user info.
   */
  async function fetchUserInfoBatch(ids) {
    const distinct = Array.from(new Set((ids || []).filter(Boolean).map(String)));

    if (distinct.length === 0) {
      return new Map();
    }

    const queryString = new URLSearchParams();
    for (const id of distinct) {
      queryString.append('ids', id);
    }

    try {
      const response = await fetch(`/api/users/userInfo?${queryString.toString()}`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return new Map();
      }

      const data = await response.json();

      if (!data) {
        return new Map();
      }

      const arr = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      const map = new Map();

      for (const user of arr) {
        const id = user?.id ?? user?.Id;
        if (!id) {
          continue;
        }
        map.set(String(id), {
          name: user?.name ?? user?.Name ?? '',
          email: user?.email ?? user?.Email ?? ''
        });
      }

      return map;
    } catch {
      return new Map();
    }
  }

  /**
   * Fetches attendee counts for all active lecture cards (called once at startup).
   */
  async function fetchActiveLectureAttendeeCounts() {
    if (!activeLecturesHost) {
      return;
    }

    const ids = Array.from(activeLectureCardsById.keys());

    if (ids.length === 0) {
      return;
    }

    for (const lectureId of ids) {
      if (attendeeTotalsInFlight.has(lectureId)) {
        continue;
      }

      attendeeTotalsInFlight.add(lectureId);

      (async function () {
        try {
          const page = await fetchLectureAttendeesPage(lectureId, 0, 1);
          const total = page?.total;
          const card = activeLectureCardsById.get(String(lectureId));

          if (card) {
            const countEl = card.querySelector('.pl-att-count');
            if (countEl && typeof total === 'number' && !Number.isNaN(total)) {
              countEl.textContent = `Attendees: ${total}`;
            }
          }

          // If popover is open for this lecture, update its header total
          try {
            if (attendeesPopoverState?.lectureId === String(lectureId) && attendeesPopoverState?.totalEl) {
              if (typeof total === 'number' && !Number.isNaN(total)) {
                attendeesPopoverState.totalKnown = total;
                updateAttendeesPopoverHeader();
                updateAttendeesLoadMoreVisibility();
              }
            }
          } catch {
            // Ignore popover update errors
          }
        } finally {
          attendeeTotalsInFlight.delete(lectureId);
        }
      })();
    }
  }

  // ============================================================================
  // Attendees Popover
  // ============================================================================

  /** The attendees popover element */
  let attendeesPopoverEl = null;

  /** Current popover state */
  let attendeesPopoverState = null;

  /**
   * Updates the visibility of the load more button.
   * @param {number|null} lastPageItemCount - Number of items in the last loaded page.
   */
  function updateAttendeesLoadMoreVisibility(lastPageItemCount = null) {
    if (!attendeesPopoverState) {
      return;
    }

    const { loadMoreBtn, loadedCount, totalKnown, pageSize } = attendeesPopoverState;

    if (!loadMoreBtn) {
      return;
    }

    const totalIsKnown = typeof totalKnown === 'number' && !Number.isNaN(totalKnown);
    const doneByTotal = totalIsKnown && loadedCount >= totalKnown;
    const doneByPage = typeof lastPageItemCount === 'number' && lastPageItemCount < pageSize;
    const done = doneByTotal || doneByPage;

    loadMoreBtn.style.display = done ? 'none' : 'flex';

    if (done) {
      loadMoreBtn.disabled = true;
    }
  }

  /**
   * Ensures the attendees popover element exists.
   * @returns {HTMLElement} The popover element.
   */
  function ensureAttendeesPopover() {
    if (attendeesPopoverEl) {
      return attendeesPopoverEl;
    }

    const pop = document.createElement('div');
    pop.id = 'attendeesModal';
    pop.className = 'att-modal';
    pop.style.display = 'none';
    pop.innerHTML = `
      <div class="att-modal-backdrop" data-close-att></div>
      <div class="att-modal-content">
        <div class="att-modal-header">
          <h2 class="att-modal-title">Attendees</h2>
          <button type="button" class="att-modal-close" data-close-att aria-label="Close">✕</button>
        </div>
        <div class="att-search-wrap">
          <input type="text" class="att-search-input" placeholder="Search by name or email..." aria-label="Search attendees" />
        </div>
        <div class="att-sub" aria-live="polite"></div>
        <div class="att-modal-body">
          <div class="att-list" role="list"></div>
        </div>
        <div class="att-modal-footer">
          <button type="button" class="att-load-more" aria-label="Load more">
            <span class="plus-icon">+</span>
            <span>Load more</span>
          </button>
        </div>
      </div>
    `;
    document.body.appendChild(pop);

    // Close button handlers
    pop.querySelectorAll('[data-close-att]').forEach(function (el) {
      el.addEventListener('click', function (event) {
        event.preventDefault();
        closeAttendeesPopover();
      });
    });

    // Clicks inside content shouldn't close
    const content = pop.querySelector('.att-modal-content');
    if (content) {
      content.addEventListener('click', function (event) {
        event.stopPropagation();
      });
    }

    // Close on escape
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && attendeesPopoverEl && attendeesPopoverEl.style.display !== 'none') {
        closeAttendeesPopover();
      }
    });

    attendeesPopoverEl = pop;
    return pop;
  }

  /**
   * Closes the attendees popover.
   */
  function closeAttendeesPopover() {
    if (!attendeesPopoverEl) {
      return;
    }

    attendeesPopoverEl.style.display = 'none';
    attendeesPopoverEl.classList.remove('open');
    document.body.style.overflow = '';
    attendeesPopoverState = null;
  }

  /**
   * Updates the attendees popover header with current counts.
   */
  function updateAttendeesPopoverHeader() {
    if (!attendeesPopoverState) {
      return;
    }

    const { subEl, loadedCount, totalKnown, titleEl, showTotalInTitle } = attendeesPopoverState;

    if (!subEl) {
      return;
    }

    const totalText = (typeof totalKnown === 'number' && !Number.isNaN(totalKnown))
      ? totalKnown
      : '—';

    subEl.textContent = `Loaded ${loadedCount} / ${totalText}`;

    // Update title with total if requested
    if (showTotalInTitle && titleEl) {
      const totalDisplay = (typeof totalKnown === 'number' && !Number.isNaN(totalKnown))
        ? totalKnown
        : '';

      titleEl.textContent = totalDisplay === ''
        ? 'Attendees'
        : `Attendees (${totalDisplay})`;
    }
  }

  /**
   * Sorts attendee items by time joined, then by user ID.
   * @param {Array} items - The attendee items.
   * @returns {Array} The sorted items.
   */
  function sortAttendeeItems(items) {
    return items.slice().sort(function (a, b) {
      const timeA = a?.timeJoined ?? a?.TimeJoined;
      const timeB = b?.timeJoined ?? b?.TimeJoined;
      const dateA = timeA ? Date.parse(timeA) : Number.NaN;
      const dateB = timeB ? Date.parse(timeB) : Number.NaN;

      if (!Number.isNaN(dateA) && !Number.isNaN(dateB) && dateA !== dateB) {
        return dateA - dateB;
      }

      const userIdA = String(a?.userId ?? a?.UserId ?? '');
      const userIdB = String(b?.userId ?? b?.UserId ?? '');
      return userIdA.localeCompare(userIdB);
    });
  }

  /**
   * Creates an attendee row element.
   * @param {Object} attendee - The attendee data.
   * @param {Map} userMap - Map of user info.
   * @returns {HTMLElement|null} The row element or null.
   */
  function createAttendeeRow(attendee, userMap) {
    const userId = String(attendee?.userId ?? attendee?.UserId ?? '');

    if (!userId) {
      return null;
    }

    const joinedRaw = attendee?.timeJoined ?? attendee?.TimeJoined;
    let joinedText = '';

    try {
      joinedText = joinedRaw ? new Date(joinedRaw).toLocaleString() : '';
    } catch {
      joinedText = '';
    }

    const info = userMap.get(userId);
    const name = info?.name || userId;
    const email = info?.email || '';

    const row = document.createElement('a');
    row.className = 'att-item';
    row.href = `/profile/view/${encodeURIComponent(userId)}`;
    row.innerHTML = `
      <div class="att-left">
        <div class="att-name">${escapeHtml(name)}</div>
        <div class="att-email">${escapeHtml(email)}</div>
      </div>
      <div class="att-time">${escapeHtml(joinedText)}</div>
    `;

    return row;
  }

  /**
   * Loads more attendees into the popover.
   */
  async function loadMoreAttendees() {
    if (!attendeesPopoverState || attendeesPopoverState.loading) {
      return;
    }

    attendeesPopoverState.loading = true;

    try {
      const { lectureId, nextPage, pageSize, listEl, loadMoreBtn, searchFilter } = attendeesPopoverState;

      if (!lectureId || !listEl) {
        return;
      }

      if (loadMoreBtn) {
        loadMoreBtn.disabled = true;
      }

      const page = await fetchLectureAttendeesPage(lectureId, nextPage, pageSize, searchFilter);

      if (!page) {
        return;
      }

      if (typeof page.total === 'number' && !Number.isNaN(page.total)) {
        attendeesPopoverState.totalKnown = page.total;
      }

      const rawItems = Array.isArray(page.items) ? page.items : [];
      const items = sortAttendeeItems(rawItems);

      if (items.length === 0 && attendeesPopoverState.loadedCount === 0) {
        if (attendeesPopoverState.subEl) {
          attendeesPopoverState.subEl.textContent = 'No attendees.';
        }
        updateAttendeesLoadMoreVisibility(0);
        return;
      }

      const userIds = items.map(function (x) {
        return x?.userId ?? x?.UserId;
      }).filter(Boolean);

      const userMap = await fetchUserInfoBatch(userIds);

      let added = 0;

      for (const attendee of items) {
        const row = createAttendeeRow(attendee, userMap);

        if (row) {
          listEl.appendChild(row);
          attendeesPopoverState.loadedCount++;
          added++;
        }
      }

      attendeesPopoverState.nextPage = nextPage + 1;
      updateAttendeesPopoverHeader();
      updateAttendeesLoadMoreVisibility(added);
    } finally {
      if (attendeesPopoverState) {
        attendeesPopoverState.loading = false;

        if (attendeesPopoverState.loadMoreBtn && attendeesPopoverState.loadMoreBtn.style.display !== 'none') {
          attendeesPopoverState.loadMoreBtn.disabled = false;
        }
      }
    }
  }

  /**
   * Opens the attendees popover for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {HTMLElement|null} anchorEl - The anchor element (unused, modal is centered).
   * @param {boolean} showTotalInTitle - Whether to show total in title.
   */
  function openAttendeesPopover(lectureId, anchorEl, showTotalInTitle = false) {
    const pop = ensureAttendeesPopover();
    const listEl = pop.querySelector('.att-list');
    const subEl = pop.querySelector('.att-sub');
    const loadMoreBtn = pop.querySelector('.att-load-more');
    const searchInput = pop.querySelector('.att-search-input');
    const titleEl = pop.querySelector('.att-modal-title');

    if (listEl) {
      listEl.innerHTML = '';
    }
    if (subEl) {
      subEl.textContent = 'Loading…';
    }
    if (searchInput) {
      searchInput.value = '';
    }
    if (titleEl) {
      titleEl.textContent = 'Attendees';
    }
    if (loadMoreBtn) {
      loadMoreBtn.disabled = false;
      loadMoreBtn.style.display = 'flex';
      loadMoreBtn.onclick = function (event) {
        event.preventDefault();
        loadMoreAttendees();
      };
    }

    attendeesPopoverState = {
      lectureId: String(lectureId),
      nextPage: 0,
      pageSize: ATTENDEE_PAGE_SIZE,
      loadedCount: 0,
      totalKnown: null,
      loading: false,
      listEl: listEl,
      subEl: subEl,
      loadMoreBtn: loadMoreBtn,
      totalEl: subEl,
      titleEl: titleEl,
      showTotalInTitle: showTotalInTitle,
      searchFilter: '',
      searchDebounceTimer: null
    };

    // Setup search input with debounce
    if (searchInput) {
      searchInput.oninput = function (event) {
        if (!attendeesPopoverState) {
          return;
        }

        const value = event.target.value;

        // Clear previous debounce timer
        if (attendeesPopoverState.searchDebounceTimer) {
          clearTimeout(attendeesPopoverState.searchDebounceTimer);
        }

        // Set new debounce timer
        attendeesPopoverState.searchDebounceTimer = setTimeout(function () {
          if (!attendeesPopoverState) {
            return;
          }
          attendeesPopoverState.searchFilter = value;
          attendeesPopoverState.nextPage = 0;
          attendeesPopoverState.loadedCount = 0;
          attendeesPopoverState.totalKnown = null;

          if (listEl) {
            listEl.innerHTML = '';
          }
          if (subEl) {
            subEl.textContent = 'Searching…';
          }

          loadMoreAttendees();
        }, SEARCH_DEBOUNCE_MS);
      };
    }

    updateAttendeesPopoverHeader();

    pop.style.display = 'flex';
    pop.classList.add('open');
    document.body.style.overflow = 'hidden';

    // Load first page
    loadMoreAttendees();
  }

  // ============================================================================
  // Quiz Selection Modal
  // ============================================================================

  /** The quiz selection modal element */
  let quizModalEl = null;

  /** Current quiz modal state */
  let quizModalState = null;

  /**
   * Fetches a page of quizzes for the current professor.
   * @param {number} page - The page number.
   * @param {number} pageSize - The page size.
   * @param {string|null} searchFilter - Optional search filter.
   * @returns {Promise<{items: Array, total: number}|null>} The page data or null.
   */
  async function fetchMyQuizzesPage(page, pageSize, searchFilter = null) {
    let baseUrl = `/api/quizzes/me?page=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`;

    if (searchFilter?.trim()) {
      baseUrl += `&name=${encodeURIComponent(searchFilter.trim())}`;
    }

    try {
      const response = await fetch(baseUrl, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return null;
      }

      const data = await response.json();
      const items = data?.items ?? data?.Items ?? data ?? [];
      const total = data?.total ?? data?.Total ?? (Array.isArray(items) ? items.length : 0);

      return {
        items: Array.isArray(items) ? items : [],
        total: Number(total || 0)
      };
    } catch {
      return null;
    }
  }

  /**
   * Activates a quiz for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {string} quizId - The quiz ID.
   * @returns {Promise<{success: boolean, error?: string}>} The result.
   */
  async function activateQuizForLecture(lectureId, quizId) {
    try {
      const response = await fetch('/api/quizzes/activate', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          lectureId: lectureId,
          quizId: quizId
        })
      });

      if (!response.ok) {
        let errorText = `Error ${response.status}`;

        try {
          const json = await response.json();
          if (json?.detail) {
            errorText = json.detail;
          } else if (json?.message) {
            errorText = json.message;
          }
        } catch {
          try {
            errorText = await response.text();
          } catch {
            // Use default error
          }
        }

        return { success: false, error: errorText };
      }

      return { success: true };
    } catch (error) {
      console.error('[ProfessorHome] Activate quiz error:', error);
      return { success: false, error: 'Network error' };
    }
  }

  /**
   * Ensures the quiz selection modal element exists.
   * @returns {HTMLElement} The modal element.
   */
  function ensureQuizModal() {
    if (quizModalEl) {
      return quizModalEl;
    }

    const modal = document.createElement('div');
    modal.id = 'quizSelectionModal';
    modal.className = 'att-modal';
    modal.style.display = 'none';
    modal.innerHTML = `
      <div class="att-modal-backdrop" data-close-quiz></div>
      <div class="att-modal-content">
        <div class="att-modal-header">
          <h2 class="att-modal-title">Select Quiz</h2>
          <button type="button" class="att-modal-close" data-close-quiz aria-label="Close">✕</button>
        </div>
        <div class="att-search-wrap">
          <input type="text" class="att-search-input quiz-search-input" placeholder="Search quizzes by name..." aria-label="Search quizzes" />
        </div>
        <div class="quiz-feedback" aria-live="polite"></div>
        <div class="att-sub quiz-sub" aria-live="polite"></div>
        <div class="att-modal-body">
          <div class="att-list quiz-list" role="list"></div>
        </div>
        <div class="att-modal-footer">
          <button type="button" class="att-load-more quiz-load-more" aria-label="Load more">
            <span class="plus-icon">+</span>
            <span>Load more</span>
          </button>
        </div>
      </div>
    `;
    document.body.appendChild(modal);

    // Close button handlers
    modal.querySelectorAll('[data-close-quiz]').forEach(function (el) {
      el.addEventListener('click', function (event) {
        event.preventDefault();
        closeQuizModal();
      });
    });

    // Clicks inside content shouldn't close
    const content = modal.querySelector('.att-modal-content');
    if (content) {
      content.addEventListener('click', function (event) {
        event.stopPropagation();
      });
    }

    // Close on escape
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && quizModalEl && quizModalEl.style.display !== 'none') {
        closeQuizModal();
      }
    });

    quizModalEl = modal;
    return modal;
  }

  /**
   * Closes the quiz selection modal.
   */
  function closeQuizModal() {
    if (!quizModalEl) {
      return;
    }

    quizModalEl.style.display = 'none';
    quizModalEl.classList.remove('open');
    document.body.style.overflow = '';
    quizModalState = null;
  }

  /**
   * Updates the quiz modal header with current counts.
   */
  function updateQuizModalHeader() {
    if (!quizModalState) {
      return;
    }

    const { subEl, loadedCount, totalKnown } = quizModalState;

    if (!subEl) {
      return;
    }

    const totalText = (typeof totalKnown === 'number' && !Number.isNaN(totalKnown))
      ? totalKnown
      : '—';

    subEl.textContent = `Loaded ${loadedCount} / ${totalText}`;
  }

  /**
   * Updates the visibility of the load more button for quizzes.
   * @param {number|null} lastPageItemCount - Number of items in the last loaded page.
   */
  function updateQuizLoadMoreVisibility(lastPageItemCount = null) {
    if (!quizModalState) {
      return;
    }

    const { loadMoreBtn, loadedCount, totalKnown, pageSize } = quizModalState;

    if (!loadMoreBtn) {
      return;
    }

    const totalIsKnown = typeof totalKnown === 'number' && !Number.isNaN(totalKnown);
    const doneByTotal = totalIsKnown && loadedCount >= totalKnown;
    const doneByPage = typeof lastPageItemCount === 'number' && lastPageItemCount < pageSize;
    const done = doneByTotal || doneByPage;

    loadMoreBtn.style.display = done ? 'none' : 'flex';

    if (done) {
      loadMoreBtn.disabled = true;
    }
  }

  /**
   * Formats a duration TimeSpan for display.
   * @param {string|null} duration - The duration string.
   * @returns {string} The formatted duration.
   */
  function formatQuizDuration(duration) {
    if (!duration) {
      return '';
    }

    const str = String(duration).trim();

    // hh:mm:ss format
    const hhmmss = /^(\d{1,2}):(\d{2}):(\d{2})$/.exec(str);
    if (hhmmss) {
      const hours = Number(hhmmss[1]);
      const mins = Number(hhmmss[2]);
      if (hours > 0) {
        return `${hours}h ${mins}m`;
      }
      return `${mins}m`;
    }

    return str;
  }

  /**
   * Creates a quiz row element for the modal.
   * @param {Object} quiz - The quiz data.
   * @param {string} lectureId - The lecture ID to activate the quiz for.
   * @returns {HTMLElement|null} The row element or null.
   */
  function createQuizRow(quiz, lectureId) {
    const quizId = String(quiz?.id ?? quiz?.Id ?? '');

    if (!quizId) {
      return null;
    }

    const name = quiz?.name ?? quiz?.Name ?? 'Untitled Quiz';
    const duration = quiz?.duration ?? quiz?.Duration;
    const durationText = formatQuizDuration(duration);
    const questionCount = quiz?.questionCount ?? quiz?.QuestionCount ?? quiz?.questions?.length ?? 0;

    const row = document.createElement('div');
    row.className = 'att-item quiz-item';
    row.style.cursor = 'pointer';
    row.innerHTML = `
      <div class="att-left">
        <div class="att-name">${escapeHtml(name)}</div>
        <div class="att-email">${escapeHtml(durationText)}${durationText && questionCount ? ' • ' : ''}${questionCount ? questionCount + ' question(s)' : ''}</div>
      </div>
    `;

    // Handle row click
    row.addEventListener('click', async function (event) {
      event.preventDefault();
      event.stopPropagation();

      // Prevent double-click
      if (row.dataset.loading === 'true') {
        return;
      }
      row.dataset.loading = 'true';
      row.style.opacity = '0.6';

      const feedbackEl = quizModalEl?.querySelector('.quiz-feedback');

      const result = await activateQuizForLecture(lectureId, quizId);

      if (result.success) {
        if (feedbackEl) {
          feedbackEl.textContent = 'Quiz started!';
          feedbackEl.className = 'quiz-feedback success';
        }
        // Refresh the active quiz progress bar
        refreshActiveQuizForLecture(lectureId);
        setTimeout(closeQuizModal, MODAL_CLOSE_DELAY_MS);
      } else {
        if (feedbackEl) {
          feedbackEl.textContent = result.error || 'Failed to start quiz';
          feedbackEl.className = 'quiz-feedback error';
        }
        row.dataset.loading = 'false';
        row.style.opacity = '1';
      }
    });

    return row;
  }

  /**
   * Loads more quizzes into the modal.
   */
  async function loadMoreQuizzes() {
    if (!quizModalState || quizModalState.loading) {
      return;
    }

    quizModalState.loading = true;

    try {
      const { lectureId, nextPage, pageSize, listEl, loadMoreBtn, searchFilter } = quizModalState;

      if (!lectureId || !listEl) {
        return;
      }

      if (loadMoreBtn) {
        loadMoreBtn.disabled = true;
      }

      const page = await fetchMyQuizzesPage(nextPage, pageSize, searchFilter);

      if (!page) {
        return;
      }

      if (typeof page.total === 'number' && !Number.isNaN(page.total)) {
        quizModalState.totalKnown = page.total;
      }

      const items = Array.isArray(page.items) ? page.items : [];

      if (items.length === 0 && quizModalState.loadedCount === 0) {
        if (quizModalState.subEl) {
          quizModalState.subEl.textContent = 'No quizzes found.';
        }
        updateQuizLoadMoreVisibility(0);
        return;
      }

      let added = 0;

      for (const quiz of items) {
        const row = createQuizRow(quiz, lectureId);

        if (row) {
          listEl.appendChild(row);
          quizModalState.loadedCount++;
          added++;
        }
      }

      quizModalState.nextPage = nextPage + 1;
      updateQuizModalHeader();
      updateQuizLoadMoreVisibility(added);
    } finally {
      if (quizModalState) {
        quizModalState.loading = false;

        if (quizModalState.loadMoreBtn && quizModalState.loadMoreBtn.style.display !== 'none') {
          quizModalState.loadMoreBtn.disabled = false;
        }
      }
    }
  }

  /**
   * Opens the quiz selection modal for a lecture.
   * @param {string} lectureId - The lecture ID.
   */
  function openQuizSelectionModal(lectureId) {
    const modal = ensureQuizModal();
    const listEl = modal.querySelector('.quiz-list');
    const subEl = modal.querySelector('.quiz-sub');
    const loadMoreBtn = modal.querySelector('.quiz-load-more');
    const searchInput = modal.querySelector('.quiz-search-input');
    const feedbackEl = modal.querySelector('.quiz-feedback');

    if (listEl) {
      listEl.innerHTML = '';
    }
    if (subEl) {
      subEl.textContent = 'Loading…';
    }
    if (searchInput) {
      searchInput.value = '';
    }
    if (feedbackEl) {
      feedbackEl.textContent = '';
      feedbackEl.className = 'quiz-feedback';
    }
    if (loadMoreBtn) {
      loadMoreBtn.disabled = false;
      loadMoreBtn.style.display = 'flex';
      loadMoreBtn.onclick = function (event) {
        event.preventDefault();
        loadMoreQuizzes();
      };
    }

    quizModalState = {
      lectureId: String(lectureId),
      nextPage: 0,
      pageSize: QUIZ_PAGE_SIZE,
      loadedCount: 0,
      totalKnown: null,
      loading: false,
      listEl: listEl,
      subEl: subEl,
      loadMoreBtn: loadMoreBtn,
      searchFilter: '',
      searchDebounceTimer: null
    };

    // Setup search input with debounce
    if (searchInput) {
      searchInput.oninput = function (event) {
        if (!quizModalState) {
          return;
        }

        const value = event.target.value;

        // Clear previous debounce timer
        if (quizModalState.searchDebounceTimer) {
          clearTimeout(quizModalState.searchDebounceTimer);
        }

        // Set new debounce timer
        quizModalState.searchDebounceTimer = setTimeout(function () {
          if (!quizModalState) {
            return;
          }
          quizModalState.searchFilter = value;
          quizModalState.nextPage = 0;
          quizModalState.loadedCount = 0;
          quizModalState.totalKnown = null;

          if (listEl) {
            listEl.innerHTML = '';
          }
          if (subEl) {
            subEl.textContent = 'Searching…';
          }
          if (feedbackEl) {
            feedbackEl.textContent = '';
            feedbackEl.className = 'quiz-feedback';
          }

          loadMoreQuizzes();
        }, SEARCH_DEBOUNCE_MS);
      };
    }

    updateQuizModalHeader();

    modal.style.display = 'flex';
    modal.classList.add('open');
    document.body.style.overflow = 'hidden';

    // Load first page
    loadMoreQuizzes();
  }

  // ============================================================================
  // Active Quiz Progress
  // ============================================================================

  /** Map of lecture ID to active quiz timer */
  const activeQuizTimers = new Map();

  /** Map of lecture ID to active quiz data */
  const activeQuizData = new Map();

  /**
   * Fetches the active quiz for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {Promise<Object|null>} The active quiz data or null.
   */
  async function fetchActiveQuizForLecture(lectureId) {
    try {
      const response = await fetch(`/api/quizzes/lecture/${encodeURIComponent(String(lectureId))}/active`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return null;
      }

      const data = await response.json();
      return data || null;
    } catch {
      return null;
    }
  }

  /**
   * Parses a duration string to milliseconds.
   * @param {string|null} duration - The duration string (hh:mm:ss or similar).
   * @returns {number} The duration in milliseconds.
   */
  function parseDurationToMs(duration) {
    if (!duration) {
      return 0;
    }

    const str = String(duration).trim();

    // hh:mm:ss format
    const hhmmss = /^(\d{1,2}):(\d{2}):(\d{2})$/.exec(str);
    if (hhmmss) {
      const hours = Number(hhmmss[1]);
      const mins = Number(hhmmss[2]);
      const secs = Number(hhmmss[3]);
      return (hours * 3600 + mins * 60 + secs) * 1000;
    }

    // hh:mm format
    const hhmm = /^(\d{1,2}):(\d{2})$/.exec(str);
    if (hhmm) {
      const hours = Number(hhmm[1]);
      const mins = Number(hhmm[2]);
      return (hours * 3600 + mins * 60) * 1000;
    }

    return 0;
  }

  /**
   * Formats remaining time in a human-readable way.
   * @param {number} ms - Milliseconds remaining.
   * @returns {string} The formatted time string.
   */
  function formatRemainingTime(ms) {
    if (ms <= 0) {
      return 'Ended';
    }

    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const mins = Math.floor((totalSeconds % 3600) / 60);
    const secs = totalSeconds % 60;

    if (hours > 0) {
      return `${hours}h ${mins}m left`;
    }
    if (mins > 0) {
      return `${mins}m ${secs}s left`;
    }
    return `${secs}s left`;
  }

  /**
   * Updates the quiz progress bar for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {HTMLElement} card - The lecture card element.
   */
  function updateQuizProgress(lectureId, card) {
    const quiz = activeQuizData.get(String(lectureId));
    const progressEl = card.querySelector('.pl-quiz-progress');

    if (!quiz || !progressEl) {
      if (progressEl) {
        progressEl.style.display = 'none';
      }
      return;
    }

    const activatedAt = quiz.activatedAtUtc ?? quiz.ActivatedAtUtc ?? quiz.activatedAt ?? quiz.ActivatedAt ?? quiz.startedAt ?? quiz.StartedAt;
    const endTimeUtc = quiz.endTimeUtc ?? quiz.EndTimeUtc;
    const duration = quiz.duration ?? quiz.Duration;
    const quizName = quiz.name ?? quiz.Name ?? 'Quiz';

    // We need either activatedAt + duration, or activatedAt + endTimeUtc
    if (!activatedAt) {
      progressEl.style.display = 'none';
      return;
    }

    const startTime = new Date(activatedAt);
    let endTime;
    let totalMs;

    if (endTimeUtc) {
      // Use endTimeUtc directly if available
      endTime = new Date(endTimeUtc);
      totalMs = Math.max(1, endTime.getTime() - startTime.getTime());
    } else if (duration) {
      // Calculate from duration
      const durationMs = parseDurationToMs(duration);
      totalMs = Math.max(1, durationMs);
      endTime = new Date(startTime.getTime() + totalMs);
    } else {
      progressEl.style.display = 'none';
      return;
    }

    const now = new Date();
    const elapsedMs = now.getTime() - startTime.getTime();
    const remainingMs = endTime.getTime() - now.getTime();

    // If quiz has ended, hide progress and clear timer
    if (remainingMs <= 0) {
      progressEl.style.display = 'none';
      clearQuizTimer(lectureId);
      activeQuizData.delete(String(lectureId));
      return;
    }

    // Show progress
    progressEl.style.display = 'block';

    const nameEl = progressEl.querySelector('.pl-quiz-progress__name');
    const remainingEl = progressEl.querySelector('.pl-quiz-progress__remaining');
    const fillEl = progressEl.querySelector('.pl-quiz-progress__fill');
    const nowEl = progressEl.querySelector('.pl-quiz-progress__now');
    const submissionsCountEl = progressEl.querySelector('.pl-quiz-progress__submissions-count');

    if (nameEl) {
      nameEl.textContent = quizName;
    }

    if (remainingEl) {
      remainingEl.textContent = formatRemainingTime(remainingMs);
    }

    if (submissionsCountEl) {
      submissionsCountEl.textContent = String(quiz.submissionCount ?? 0);
    }

    const clampedMs = Math.max(0, Math.min(totalMs, elapsedMs));
    const progressPercent = Math.max(0, Math.min(100, (clampedMs / totalMs) * 100));

    if (fillEl) {
      fillEl.style.width = `${progressPercent}%`;
    }

    if (nowEl) {
      nowEl.style.left = `${progressPercent}%`;
    }
  }

  /**
   * Clears the quiz progress timer for a lecture.
   * @param {string} lectureId - The lecture ID.
   */
  function clearQuizTimer(lectureId) {
    const key = String(lectureId);
    const timer = activeQuizTimers.get(key);
    if (timer) {
      clearInterval(timer);
      activeQuizTimers.delete(key);
    }
  }

  /**
   * Fetches and displays the active quiz progress for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {HTMLElement} card - The lecture card element.
   */
  async function fetchAndDisplayActiveQuiz(lectureId, card) {
    const key = String(lectureId);

    try {
      const quiz = await fetchActiveQuizForLecture(lectureId);

      if (!quiz) {
        // No active quiz
        activeQuizData.delete(key);
        clearQuizTimer(key);
        const progressEl = card.querySelector('.pl-quiz-progress');
        if (progressEl) {
          progressEl.style.display = 'none';
        }
        return;
      }

      // Fetch submission count
      const quizLectureId = quiz.quizLectureId ?? quiz.QuizLectureId;
      if (quizLectureId) {
        try {
          const count = await fetchSubmissionCount(quizLectureId);
          quiz.submissionCount = count;
        } catch {
          quiz.submissionCount = 0;
        }
      }

      // Store quiz data
      activeQuizData.set(key, quiz);

      // Initial render
      updateQuizProgress(lectureId, card);

      // Clear existing timer if any
      clearQuizTimer(key);

      // Start update timer
      const timer = setInterval(function () {
        // Check if card still exists
        if (!activeLectureCardsById.has(key)) {
          clearQuizTimer(key);
          return;
        }
        updateQuizProgress(lectureId, card);
      }, QUIZ_PROGRESS_UPDATE_INTERVAL_MS);

      activeQuizTimers.set(key, timer);
    } catch (error) {
      console.error('[ProfessorHome] Failed to fetch active quiz:', error);
    }
  }

  /**
   * Fetches the submission count for a quiz lecture.
   * @param {string} quizLectureId - The quiz lecture ID.
   * @returns {Promise<number>} The submission count.
   */
  async function fetchSubmissionCount(quizLectureId) {
    try {
      const response = await fetch(`/api/quizzes/quiz-lecture/${encodeURIComponent(String(quizLectureId))}/submissions/count`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return 0;
      }

      const count = await response.json();
      return typeof count === 'number' ? count : 0;
    } catch {
      return 0;
    }
  }

  /**
   * Refreshes the active quiz display after starting a new quiz.
   * @param {string} lectureId - The lecture ID.
   */
  function refreshActiveQuizForLecture(lectureId) {
    const key = String(lectureId);
    const card = activeLectureCardsById.get(key);
    if (card) {
      fetchAndDisplayActiveQuiz(lectureId, card);
    }
  }

  // ============================================================================
  // Calendar Status Pill Updates
  // ============================================================================

  /**
   * Updates status pills in the calendar for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @param {number|string} statusVal - The new status value.
   */
  function updateRenderedCalendarStatusPill(lectureId, statusVal) {
    if (!lectureId) {
      return;
    }

    try {
      const raw = String(lectureId);
      const escaped = (typeof CSS !== 'undefined' && CSS.escape)
        ? CSS.escape(raw)
        : raw.replaceAll('\\', '\\\\').replaceAll('"', '\\"');

      const pills = document.querySelectorAll(`.event-status-pill[data-ev-id="${escaped}"]`);

      if (!pills || pills.length === 0) {
        return;
      }

      const label = statusToLabel(statusVal);
      const normalized = normalizeStatus(statusVal);

      let cssClass = null;
      if (normalized === LectureStatus.SCHEDULED || normalized === 'scheduled') {
        cssClass = 'status-scheduled';
      } else if (normalized === LectureStatus.IN_PROGRESS || normalized === 'inprogress') {
        cssClass = 'status-inprogress';
      } else if (normalized === LectureStatus.ENDED || normalized === 'ended') {
        cssClass = 'status-ended';
      } else if (normalized === LectureStatus.CANCELED || normalized === 'canceled') {
        cssClass = 'status-canceled';
      }

      pills.forEach(function (pill) {
        pill.classList.remove('status-scheduled', 'status-inprogress', 'status-ended', 'status-canceled');

        if (cssClass) {
          pill.classList.add(cssClass);
        }

        pill.textContent = label;
      });
    } catch {
      // Ignore status pill update errors
    }
  }

  // ============================================================================
  // Calendar Registration
  // ============================================================================

  /**
   * Registers with the calendar component for lazy loading.
   */
  (function ensureCalendarRegistration() {
    let checks = 0;

    const interval = setInterval(function () {
      checks++;

      if (globalThis.AttendanceCalendar && typeof globalThis.AttendanceCalendar.onViewChange === 'function') {
        clearInterval(interval);

        // Register loader for view changes
        globalThis.AttendanceCalendar.onViewChange(async function (monthKey, viewDate) {
          try {
            if (typeof globalThis.AttendanceCalendar.isMonthLoaded === 'function' &&
                globalThis.AttendanceCalendar.isMonthLoaded(monthKey)) {
              return;
            }

            // Mark as loading to prevent duplicate concurrent loads
            if (typeof globalThis.AttendanceCalendar.markMonthLoading === 'function') {
              globalThis.AttendanceCalendar.markMonthLoading(monthKey);
            }

            const now = new Date();
            const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) -
                              (viewDate.getFullYear() * 12 + viewDate.getMonth());

            await loadProfessorLectures({ fromMonthsAgo: monthsAgo });

            if (typeof globalThis.AttendanceCalendar.markMonthLoaded === 'function') {
              globalThis.AttendanceCalendar.markMonthLoaded(monthKey);
            }
          } catch (error) {
            console.error('[ProfessorHome] Calendar view change error:', error);
          }
        });

        // Initial load for previous, current and next months
        (async function () {
          try {
            const now = new Date();
            const cur = new Date(now.getFullYear(), now.getMonth(), 1);
            const prev = new Date(cur.getFullYear(), cur.getMonth() - 1, 1);
            const next = new Date(cur.getFullYear(), cur.getMonth() + 1, 1);
            const months = [prev, cur, next];

            for (const monthDate of months) {
              const monthKey = `${monthDate.getFullYear()}-${monthDate.getMonth() + 1}`;

              if (typeof globalThis.AttendanceCalendar.isMonthLoaded === 'function' &&
                  globalThis.AttendanceCalendar.isMonthLoaded(monthKey)) {
                continue;
              }

              if (typeof globalThis.AttendanceCalendar.markMonthLoading === 'function') {
                globalThis.AttendanceCalendar.markMonthLoading(monthKey);
              }

              const monthsAgo = (now.getFullYear() * 12 + now.getMonth()) -
                                (monthDate.getFullYear() * 12 + monthDate.getMonth());

              await loadProfessorLectures({ fromMonthsAgo: monthsAgo });

              if (typeof globalThis.AttendanceCalendar.markMonthLoaded === 'function') {
                globalThis.AttendanceCalendar.markMonthLoaded(monthKey);
              }
            }
          } catch (error) {
            console.error('[ProfessorHome] Initial calendar load error:', error);
          }
        })();
      }

      if (checks >= CALENDAR_MAX_CHECKS) {
        clearInterval(interval);
      }
    }, CALENDAR_CHECK_INTERVAL_MS);
  })();

  // ============================================================================
  // Lecture Popup (Calendar Event Click Handler)
  // ============================================================================

  // Map to track lecture statuses by id
  globalThis._lectureStatusMap = globalThis._lectureStatusMap || {};

  /**
   * Creates an action button for the lecture popup.
   * @param {string} label - The button label.
   * @param {string} cssClass - The CSS class.
   * @param {Function} handler - The click handler.
   * @returns {HTMLElement} The button element.
   */
  function createActionButton(label, cssClass, handler) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = cssClass || 'btn-inline';
    button.textContent = label;
    button.addEventListener('click', handler);
    return button;
  }

  /**
   * Creates the lecture popup element if it doesn't exist.
   * @returns {HTMLElement} The popup element.
   */
  function ensureLecturePopup() {
    let popup = document.getElementById('lecturePopup');

    if (!popup) {
      popup = document.createElement('div');
      popup.id = 'lecturePopup';
      popup.className = 'lecture-popup';
      popup.innerHTML = `
        <button type="button" class="lp-close" aria-label="Close">×</button>
        <div class="lp-body">
          <div class="lp-title"></div>
          <div class="lp-time small text-muted"></div>
          <div class="lp-row" style="display:flex;align-items:center;gap:8px;">
            <div class="lp-desc small" style="flex:1"></div>
          </div>
          <div class="lp-status small mt-1"></div>
          <div class="lp-quizzes mt-2">
            <div class="lp-quizzes__label small text-muted">Quizzes:</div>
            <div class="lp-quizzes__list"></div>
          </div>
          <div class="lp-actions mt-2"></div>
        </div>
      `;
      document.body.appendChild(popup);

      // Close on clicking the close button
      const closeBtn = popup.querySelector('.lp-close');
      if (closeBtn) {
        closeBtn.addEventListener('click', function (event) {
          event.stopPropagation();
          try {
            popup.style.display = 'none';
          } catch {
            // Ignore close errors
          }
        });
      }

      // Stop clicks inside popup from bubbling
      popup.addEventListener('click', function (event) {
        event.stopPropagation();
      });

      // Close on outside click
      document.addEventListener('click', function (event) {
        if (!popup.contains(event.target)) {
          popup.style.display = 'none';
        }
      });
    }

    return popup;
  }

  /**
   * Positions the lecture popup relative to an element.
   * @param {HTMLElement} popup - The popup element.
   * @param {HTMLElement|null} anchorEl - The anchor element.
   */
  function positionLecturePopup(popup, anchorEl) {
    popup.style.display = 'block';
    popup.style.position = 'fixed';
    popup.style.zIndex = '20000';

    if (anchorEl) {
      const rect = anchorEl.getBoundingClientRect();
      const top = rect.top - popup.offsetHeight - 8;
      const left = rect.left + (rect.width - popup.offsetWidth) / 2;
      popup.style.top = `${Math.max(8, top)}px`;
      popup.style.left = `${Math.max(8, left)}px`;
    } else {
      // Fallback: position near calendar popover
      const calendarPopover = document.getElementById('calendarPopover');
      if (calendarPopover) {
        const popRect = calendarPopover.getBoundingClientRect();
        const top = popRect.top - popup.offsetHeight - 8;
        const left = popRect.left + (popRect.width - popup.offsetWidth) / 2;
        popup.style.top = `${Math.max(8, top)}px`;
        popup.style.left = `${Math.max(8, left)}px`;
      }
    }
  }

  /**
   * Fetches quizzes for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {Promise<Array>} The quizzes.
   */
  async function fetchQuizzesForLecture(lectureId) {
    try {
      const response = await fetch(`/api/quizzes/lecture/${lectureId}`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' }
      });
      if (!response.ok) {
        return [];
      }
      return await response.json();
    } catch {
      return [];
    }
  }

  /**
   * Fetches quiz info by ID.
   * @param {string} quizId - The quiz ID.
   * @returns {Promise<Object|null>} The quiz info.
   */
  async function fetchQuizInfo(quizId) {
    try {
      const response = await fetch(`/api/quizzes/${quizId}`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' }
      });
      if (!response.ok) {
        return null;
      }
      return await response.json();
    } catch {
      return null;
    }
  }

  /**
   * Loads and displays quizzes in the lecture popup.
   * @param {string} lectureId - The lecture ID.
   * @param {HTMLElement} popup - The popup element.
   */
  async function loadQuizzesInPopup(lectureId, popup) {
    const quizzesSection = popup.querySelector('.lp-quizzes');
    const quizzesList = popup.querySelector('.lp-quizzes__list');

    if (!quizzesSection || !quizzesList) {
      return;
    }

    quizzesList.innerHTML = '<span class="small text-muted">Loading...</span>';
    quizzesSection.style.display = 'block';

    try {
      const quizLectures = await fetchQuizzesForLecture(lectureId);

      if (!quizLectures || quizLectures.length === 0) {
        quizzesSection.style.display = 'none';
        return;
      }

      // Fetch quiz info for each quiz lecture
      const quizInfoPromises = quizLectures.map(async (ql) => {
        const quizId = ql.quizId ?? ql.QuizId;
        const quizInfo = await fetchQuizInfo(quizId);
        return {
          quizLectureId: ql.id ?? ql.Id,
          quizId: quizId,
          name: quizInfo?.name ?? quizInfo?.Name ?? 'Quiz',
          createdAtUtc: ql.createdAtUtc ?? ql.CreatedAtUtc
        };
      });

      const quizzes = await Promise.all(quizInfoPromises);

      quizzesList.innerHTML = '';

      quizzes.forEach(function (quiz) {
        const quizItem = document.createElement('div');
        quizItem.className = 'lp-quiz-item';

        const nameSpan = document.createElement('span');
        nameSpan.className = 'lp-quiz-item__name';
        nameSpan.textContent = quiz.name;

        const viewBtn = document.createElement('button');
        viewBtn.type = 'button';
        viewBtn.className = 'btn-inline btn-sm';
        viewBtn.textContent = 'Results';
        viewBtn.addEventListener('click', function () {
          popup.style.display = 'none';
          openQuizResultsModal(quiz.quizLectureId, quiz.name);
        });

        quizItem.appendChild(nameSpan);
        quizItem.appendChild(viewBtn);
        quizzesList.appendChild(quizItem);
      });
    } catch (error) {
      console.error('[ProfessorHome] Failed to load quizzes:', error);
      quizzesSection.style.display = 'none';
    }
  }

  /**
   * Handles calendar event clicks to show the lecture popup.
   * @param {Object} ev - The event object.
   */
  globalThis.onCalendarEventClick = function (ev) {
    try {
      // Find the DOM node for this event
      const eventEl = document.querySelector(`[data-event-id="${ev.id}"]`);
      const popup = ensureLecturePopup();

      const titleEl = popup.querySelector('.lp-title');
      const timeEl = popup.querySelector('.lp-time');
      const descEl = popup.querySelector('.lp-desc');
      const statusEl = popup.querySelector('.lp-status');
      const actionsEl = popup.querySelector('.lp-actions');

      titleEl.textContent = ev.title || '';
      descEl.textContent = ev.description || '';
      timeEl.textContent = `${new Date(ev.start).toLocaleString()} — ${new Date(ev.end).toLocaleString()}`;

      const id = ev.id;
      const current = (globalThis._lectureStatusMap?.[id] !== undefined)
        ? globalThis._lectureStatusMap[id]
        : (ev.status ?? ev.Status);

      statusEl.textContent = `Status: ${statusToLabel(current)}`;

      // Clear previous action buttons
      try {
        actionsEl.innerHTML = '';
      } catch {
        // Ignore clear errors
      }

      const currentNormalized = normalizeStatus(current);

      if (currentNormalized === LectureStatus.SCHEDULED || currentNormalized === 'scheduled') {
        actionsEl.appendChild(createActionButton('Start', 'btn-inline', async function () {
          await doStatusChange(id, LectureStatus.IN_PROGRESS, popup);
        }));
        actionsEl.appendChild(createActionButton('Cancel', 'btn-inline', async function () {
          await doStatusChange(id, LectureStatus.CANCELED, popup);
        }));
      } else if (currentNormalized === LectureStatus.IN_PROGRESS || currentNormalized === 'inprogress') {
        actionsEl.appendChild(createActionButton('End', 'btn-inline', async function () {
          await doStatusChange(id, LectureStatus.ENDED, popup);
        }));
      } else if (currentNormalized === LectureStatus.ENDED || currentNormalized === 'ended') {
        actionsEl.appendChild(createActionButton('View Attendees', 'btn-inline', function () {
          popup.style.display = 'none';
          openAttendeesPopover(id, null, true);
        }));
      } else if (currentNormalized === LectureStatus.CANCELED || currentNormalized === 'canceled') {
        actionsEl.appendChild(createActionButton('Delete', 'btn-inline btn-danger', async function () {
          await doDeleteLecture(id, popup);
        }));
      }

      // Load quizzes for this lecture
      loadQuizzesInPopup(id, popup);

      positionLecturePopup(popup, eventEl);
    } catch (error) {
      console.error('[ProfessorHome] Calendar event click error:', error);
    }
  };

  // ============================================================================
  // Quiz Results Modal
  // ============================================================================

  /** Current quiz results modal state */
  let quizResultsModalState = {
    quizLectureId: null,
    quizName: '',
    page: 0,
    pageSize: 20,
    search: '',
    totalCount: 0
  };

  /**
   * Creates the quiz results modal if it doesn't exist.
   * @returns {HTMLElement} The modal element.
   */
  function ensureQuizResultsModal() {
    let modal = document.getElementById('quizResultsModal');

    if (!modal) {
      modal = document.createElement('div');
      modal.id = 'quizResultsModal';
      modal.className = 'quiz-results-modal';
      modal.innerHTML = `
        <div class="qr-backdrop" data-close-qr-modal></div>
        <div class="qr-dialog">
          <div class="qr-header">
            <h3 class="qr-title">Quiz Results</h3>
            <button type="button" class="qr-close" data-close-qr-modal aria-label="Close">×</button>
          </div>
          <div class="qr-search">
            <input type="text" class="qr-search__input" placeholder="Search by name or email..." />
          </div>
          <div class="qr-body">
            <div class="qr-loading">Loading...</div>
            <table class="qr-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Score</th>
                  <th>Submitted</th>
                </tr>
              </thead>
              <tbody class="qr-table__body"></tbody>
            </table>
            <div class="qr-empty" style="display:none;">No submissions found.</div>
          </div>
          <div class="qr-footer">
            <div class="qr-pagination">
              <button type="button" class="qr-pagination__prev btn-inline" disabled>Previous</button>
              <span class="qr-pagination__info">Page 1</span>
              <button type="button" class="qr-pagination__next btn-inline">Next</button>
            </div>
          </div>
        </div>
      `;
      document.body.appendChild(modal);

      // Close handlers
      modal.querySelectorAll('[data-close-qr-modal]').forEach(function (el) {
        el.addEventListener('click', function (event) {
          event.stopPropagation();
          closeQuizResultsModal();
        });
      });

      // Search input handler
      const searchInput = modal.querySelector('.qr-search__input');
      let searchTimeout = null;
      searchInput.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
          quizResultsModalState.search = searchInput.value.trim();
          quizResultsModalState.page = 0;
          loadQuizResultsPage();
        }, 300);
      });

      // Pagination handlers
      const prevBtn = modal.querySelector('.qr-pagination__prev');
      const nextBtn = modal.querySelector('.qr-pagination__next');

      prevBtn.addEventListener('click', function () {
        if (quizResultsModalState.page > 0) {
          quizResultsModalState.page--;
          loadQuizResultsPage();
        }
      });

      nextBtn.addEventListener('click', function () {
        const maxPage = Math.ceil(quizResultsModalState.totalCount / quizResultsModalState.pageSize) - 1;
        if (quizResultsModalState.page < maxPage) {
          quizResultsModalState.page++;
          loadQuizResultsPage();
        }
      });

      // Stop clicks inside dialog from bubbling
      modal.querySelector('.qr-dialog').addEventListener('click', function (event) {
        event.stopPropagation();
      });
    }

    return modal;
  }

  /**
   * Opens the quiz results modal.
   * @param {string} quizLectureId - The quiz lecture ID.
   * @param {string} quizName - The quiz name.
   */
  function openQuizResultsModal(quizLectureId, quizName) {
    const modal = ensureQuizResultsModal();

    quizResultsModalState = {
      quizLectureId: quizLectureId,
      quizName: quizName,
      page: 0,
      pageSize: 20,
      search: '',
      totalCount: 0
    };

    const titleEl = modal.querySelector('.qr-title');
    titleEl.textContent = `Results: ${quizName}`;

    const searchInput = modal.querySelector('.qr-search__input');
    searchInput.value = '';

    modal.classList.add('open');
    loadQuizResultsPage();
  }

  /**
   * Closes the quiz results modal.
   */
  function closeQuizResultsModal() {
    const modal = document.getElementById('quizResultsModal');
    if (modal) {
      modal.classList.remove('open');
    }
  }

  /**
   * Fetches quiz submissions from the API.
   * @returns {Promise<Object>} The submissions data.
   */
  async function fetchQuizSubmissions() {
    const { quizLectureId, page, pageSize, search } = quizResultsModalState;
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize)
    });
    if (search) {
      params.set('search', search);
    }

    try {
      const response = await fetch(`/api/quizzes/quiz-lecture/${quizLectureId}/submissions?${params}`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' }
      });
      if (!response.ok) {
        return { submissions: [], totalCount: 0 };
      }
      return await response.json();
    } catch {
      return { submissions: [], totalCount: 0 };
    }
  }

  /**
   * Loads and displays the current page of quiz results.
   */
  async function loadQuizResultsPage() {
    const modal = document.getElementById('quizResultsModal');
    if (!modal) return;

    const loadingEl = modal.querySelector('.qr-loading');
    const tableEl = modal.querySelector('.qr-table');
    const tbodyEl = modal.querySelector('.qr-table__body');
    const emptyEl = modal.querySelector('.qr-empty');
    const prevBtn = modal.querySelector('.qr-pagination__prev');
    const nextBtn = modal.querySelector('.qr-pagination__next');
    const pageInfo = modal.querySelector('.qr-pagination__info');

    loadingEl.style.display = 'block';
    tableEl.style.display = 'none';
    emptyEl.style.display = 'none';

    try {
      const data = await fetchQuizSubmissions();
      const submissions = data.submissions ?? data.Submissions ?? [];
      const totalCount = data.totalCount ?? data.TotalCount ?? 0;

      quizResultsModalState.totalCount = totalCount;

      loadingEl.style.display = 'none';

      if (submissions.length === 0) {
        emptyEl.style.display = 'block';
        tableEl.style.display = 'none';
      } else {
        emptyEl.style.display = 'none';
        tableEl.style.display = 'table';

        tbodyEl.innerHTML = '';
        submissions.forEach(function (sub) {
          const tr = document.createElement('tr');

          const nameTd = document.createElement('td');
          nameTd.textContent = sub.userName ?? sub.UserName ?? 'Unknown';

          const emailTd = document.createElement('td');
          emailTd.textContent = sub.userEmail ?? sub.UserEmail ?? '';

          const scoreTd = document.createElement('td');
          const score = sub.score ?? sub.Score ?? 0;
          const maxScore = sub.maxScore ?? sub.MaxScore ?? 0;
          scoreTd.textContent = `${score} / ${maxScore}`;

          const timeTd = document.createElement('td');
          const submittedAt = sub.submittedAtUtc ?? sub.SubmittedAtUtc;
          timeTd.textContent = submittedAt ? new Date(submittedAt).toLocaleString() : '-';

          tr.appendChild(nameTd);
          tr.appendChild(emailTd);
          tr.appendChild(scoreTd);
          tr.appendChild(timeTd);
          tbodyEl.appendChild(tr);
        });
      }

      // Update pagination
      const maxPage = Math.max(0, Math.ceil(totalCount / quizResultsModalState.pageSize) - 1);
      prevBtn.disabled = quizResultsModalState.page <= 0;
      nextBtn.disabled = quizResultsModalState.page >= maxPage;
      pageInfo.textContent = `Page ${quizResultsModalState.page + 1} of ${maxPage + 1}`;

    } catch (error) {
      console.error('[ProfessorHome] Failed to load quiz results:', error);
      loadingEl.style.display = 'none';
      emptyEl.textContent = 'Failed to load results.';
      emptyEl.style.display = 'block';
    }
  }

  // ============================================================================
  // Lecture Status Change
  // ============================================================================

  /**
   * Changes the status of a lecture via API.
   * @param {string} lectureId - The lecture ID.
   * @param {number} statusValue - The new status value.
   * @param {HTMLElement} popupOrFeedback - The popup or feedback element.
   * @param {HTMLElement|null} modalEl - Optional modal element.
   */
  async function doStatusChange(lectureId, statusValue, popupOrFeedback, modalEl) {
    if (!lectureId) {
      return;
    }

    const popup = (popupOrFeedback?.id === 'lecturePopup')
      ? popupOrFeedback
      : null;

    const feedbackEl = popup
      ? (popup.querySelector('.lp-actions') || popup)
      : popupOrFeedback;

    try {
      const response = await fetch(`/api/lectures/status/${lectureId}`, {
        method: 'PUT',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: statusValue })
      });

      if (!response.ok) {
        let errorText = `Error ${response.status}`;

        try {
          const json = await response.json();
          if (json && json.detail) {
            errorText = json.detail;
          }
        } catch {
          try {
            errorText = await response.text();
          } catch {
            // Use default error
          }
        }

        try {
          if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') {
            feedbackEl.textContent = errorText;
          }
        } catch {
          // Ignore feedback update errors
        }
        return;
      }

      // Success: update local status map
      let updated = null;
      try {
        updated = await response.json();
      } catch {
        // Endpoint may return no content
      }

      const newStatus = updated?.status ?? updated?.Status ?? statusValue;
      globalThis._lectureStatusMap[lectureId] = newStatus;

      // Update any already-rendered status pill in the calendar
      updateRenderedCalendarStatusPill(lectureId, newStatus);

      // Keep active lecture cards in sync
      try {
        const meta = lectureMetaById.get(String(lectureId));

        if (isActiveLectureStatus(newStatus)) {
          upsertActiveLectureCard({
            id: lectureId,
            name: updated?.name ?? updated?.Name ?? meta?.name,
            description: updated?.description ?? updated?.Description ?? meta?.description,
            status: newStatus
          });
        } else {
          removeActiveLectureCard(lectureId);
        }
      } catch {
        // Ignore card sync errors
      }

      // Hide popup or modal
      if (modalEl) {
        try {
          const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
          bsModal.hide();
        } catch {
          // Ignore modal hide errors
        }
      } else if (popup) {
        try {
          popup.style.display = 'none';
        } catch {
          // Ignore popup hide errors
        }
      }
    } catch (error) {
      console.error('[ProfessorHome] Status change error:', error);

      try {
        if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') {
          feedbackEl.textContent = 'Network error';
        }
      } catch {
        // Ignore feedback update errors
      }
    }
  }

  // ============================================================================
  // Lecture Deletion
  // ============================================================================

  /**
   * Deletes a lecture via API.
   * @param {string} lectureId - The lecture ID.
   * @param {HTMLElement} popupOrFeedback - The popup or feedback element.
   */
  async function doDeleteLecture(lectureId, popupOrFeedback) {
    if (!lectureId) {
      return;
    }

    const popup = (popupOrFeedback?.id === 'lecturePopup')
      ? popupOrFeedback
      : null;

    const feedbackEl = popup
      ? (popup.querySelector('.lp-actions') || popup)
      : popupOrFeedback;

    try {
      const response = await fetch(`/api/lectures/${lectureId}`, {
        method: 'DELETE',
        credentials: 'same-origin'
      });

      if (!response.ok) {
        let errorText = `Error ${response.status}`;

        try {
          const json = await response.json();
          if (json && json.detail) {
            errorText = json.detail;
          }
        } catch {
          try {
            errorText = await response.text();
          } catch {
            // Use default error
          }
        }

        try {
          if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') {
            feedbackEl.textContent = errorText;
          }
        } catch {
          // Ignore feedback update errors
        }
        return;
      }

      // Success: remove lecture from local state
      delete globalThis._lectureStatusMap[lectureId];

      // Remove from calendar events
      try {
        if (globalThis.AttendanceCalendar) {
          const eventsHost = document.querySelector('[data-cal-events]');
          if (eventsHost) {
            const evBlock = eventsHost.querySelector(`[data-event-id="${lectureId}"]`);
            if (evBlock) {
              evBlock.remove();
            }
          }
        }
      } catch {
        // Ignore calendar removal errors
      }

      // Remove active lecture card if present
      try {
        removeActiveLectureCard(lectureId);
      } catch {
        // Ignore card removal errors
      }

      // Remove from cached meta
      try {
        lectureMetaById.delete(String(lectureId));
      } catch {
        // Ignore meta removal errors
      }

      // Hide popup
      if (popup) {
        try {
          popup.style.display = 'none';
        } catch {
          // Ignore popup hide errors
        }
      }
    } catch (error) {
      console.error('[ProfessorHome] Delete lecture error:', error);

      try {
        if (feedbackEl && typeof feedbackEl.textContent !== 'undefined') {
          feedbackEl.textContent = 'Network error';
        }
      } catch {
        // Ignore feedback update errors
      }
    }
  }

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes modal event listeners.
   */
  function initModalEventListeners() {
    // Wire open button
    if (openBtn) {
      openBtn.addEventListener('click', function (event) {
        event.preventDefault();
        openModal();
      });
    }

    // Wire close buttons (backdrop and close button)
    modalEl.querySelectorAll('[data-close-modal]').forEach(function (el) {
      el.addEventListener('click', function (event) {
        event.preventDefault();
        closeModal();
      });
    });

    // Close on Escape key
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && modalEl.classList.contains('open')) {
        closeModal();
      }
    });

    // Form submission
    form.addEventListener('submit', handleFormSubmit);
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  initModalEventListeners();
});
