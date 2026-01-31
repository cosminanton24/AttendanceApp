/**
 * @fileoverview Student home page functionality.
 * Handles lecture joining, active lecture display, and calendar integration.
 */

'use strict';

(function () {
  // ============================================================================
  // Constants
  // ============================================================================

  /** Progress update interval in milliseconds */
  const PROGRESS_UPDATE_INTERVAL_MS = 15000;

  /** Default lecture duration in minutes */
  const DEFAULT_DURATION_MINUTES = 60;

  /** Milliseconds per minute */
  const MS_PER_MINUTE = 60 * 1000;

  /** Maximum checks for calendar availability */
  const CALENDAR_MAX_CHECKS = 200;

  /** Interval between calendar availability checks in milliseconds */
  const CALENDAR_CHECK_INTERVAL_MS = 50;

  /** Lecture status values */
  const LectureStatus = Object.freeze({
    IN_PROGRESS: 1,
    IN_PROGRESS_STRING: 'inprogress',
    IN_PROGRESS_DISPLAY: 'in progress'
  });

  /** API endpoint candidates for fetching student lectures */
  const API_ENDPOINTS = Object.freeze([
    '/api/lectures/student',
    '/api/lectrues/student',
    '/api/lectures'
  ]);

  // ============================================================================
  // State
  // ============================================================================

  const state = {
    lectureProgressTimer: null,
    activeLectureWindow: null
  };

  // Initialize global status map for calendar pills
  globalThis._lectureStatusMap = globalThis._lectureStatusMap || {};

  // ============================================================================
  // DOM Elements
  // ============================================================================

  const elements = {
    joinState: document.getElementById('joinState'),
    inClassState: document.getElementById('inClassState'),
    className: document.getElementById('className'),
    classDesc: document.getElementById('classDesc'),
    timeRange: document.getElementById('studentLectureTimeRange'),
    remaining: document.getElementById('studentLectureRemaining'),
    track: document.getElementById('studentLectureProgressTrack'),
    fill: document.getElementById('studentLectureProgressFill'),
    nowLine: document.getElementById('studentLectureNowLine'),
    joinBtn: document.getElementById('joinClassBtn'),
    classIdInput: document.getElementById('classIdInput')
  };

  // Early exit if required elements are missing
  if (!elements.joinState || !elements.inClassState || !elements.className || !elements.classDesc) {
    return;
  }

  // ============================================================================
  // Utility Functions
  // ============================================================================

  /**
   * Safely sets an attribute on an element.
   * @param {HTMLElement|null} element - The target element.
   * @param {string} name - The attribute name.
   * @param {string} value - The attribute value.
   */
  function safeSetAttribute(element, name, value) {
    if (element) {
      try {
        element.setAttribute(name, value);
      } catch {
        // Element may not support the attribute
      }
    }
  }

  /**
   * Normalizes a status value to a comparable format.
   * @param {number|string|null} status - The status value to normalize.
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
   * Checks if a lecture status indicates an active/in-progress lecture.
   * @param {number|string|null} status - The status to check.
   * @returns {boolean} True if the lecture is active.
   */
  function isActiveLectureStatus(status) {
    const normalized = normalizeStatus(status);
    return normalized === LectureStatus.IN_PROGRESS ||
           normalized === LectureStatus.IN_PROGRESS_STRING ||
           normalized === LectureStatus.IN_PROGRESS_DISPLAY;
  }

  /**
   * Gets a property from an item with case-insensitive fallback.
   * @param {Object} item - The item object.
   * @param {string} propLower - The lowercase property name.
   * @param {string} propUpper - The uppercase property name.
   * @param {*} defaultValue - The default value if not found.
   * @returns {*} The property value or default.
   */
  function getItemProperty(item, propLower, propUpper, defaultValue = null) {
    return item?.[propLower] ?? item?.[propUpper] ?? defaultValue;
  }

  // ============================================================================
  // Duration Parsing
  // ============================================================================

  /**
   * Parses ISO 8601 duration format (e.g., PT1H30M).
   * @param {string} input - The duration string in uppercase.
   * @returns {number} The duration in minutes.
   */
  function parseIsoDuration(input) {
    const hours = Number((input.match(/(\d+)H/) || [0, 0])[1] || 0);
    const minutes = Number((input.match(/(\d+)M/) || [0, 0])[1] || 0);
    const seconds = Number((input.match(/(\d+)S/) || [0, 0])[1] || 0);
    return hours * 60 + minutes + Math.round(seconds / 60);
  }

  /**
   * Parses a duration input into minutes.
   * Supports formats: ISO 8601 (PT1H30M), HH:MM:SS, HH:MM, 1h30m, 90m, 90.
   * @param {number|string|null} input - The duration input.
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

    // ISO 8601 duration (PT1H30M)
    if (/^P/i.test(str)) {
      return parseIsoDuration(str.toUpperCase());
    }

    // HH:MM:SS format
    const hhmmssMatch = str.match(/^(\d{1,2}):(\d{2}):(\d{2})$/);
    if (hhmmssMatch) {
      return Number(hhmmssMatch[1]) * 60 + Number(hhmmssMatch[2]) + Math.round(Number(hhmmssMatch[3]) / 60);
    }

    // HH:MM format
    const hhmmMatch = str.match(/^(\d{1,2}):(\d{2})$/);
    if (hhmmMatch) {
      return Number(hhmmMatch[1]) * 60 + Number(hhmmMatch[2]);
    }

    // 1h30m or 1h format
    const hmMatch = str.match(/^(\d+)h(?:([0-9]+)m)?$/i);
    if (hmMatch) {
      return Number(hmMatch[1]) * 60 + Number(hmMatch[2] || 0);
    }

    // Minutes only (90 or 90m)
    const minsMatch = str.match(/^(\d+)(?:m)?$/i);
    if (minsMatch) {
      return Number(minsMatch[1]);
    }

    return DEFAULT_DURATION_MINUTES;
  }

  // ============================================================================
  // Time Formatting
  // ============================================================================

  /**
   * Formats a time range as a string.
   * @param {Date} start - The start time.
   * @param {Date} end - The end time.
   * @returns {string} The formatted time range.
   */
  function formatTimeRange(start, end) {
    try {
      const options = { hour: '2-digit', minute: '2-digit' };
      const startStr = start.toLocaleTimeString([], options);
      const endStr = end.toLocaleTimeString([], options);
      return `${startStr} – ${endStr}`;
    } catch {
      return '';
    }
  }

  /**
   * Converts minutes to a human-readable string.
   * @param {number} mins - The number of minutes.
   * @returns {string} The humanized duration (e.g., "1h 30m").
   */
  function humanizeMinutes(mins) {
    const totalMinutes = Math.max(0, Math.round(mins));

    if (totalMinutes < 60) {
      return `${totalMinutes}m`;
    }

    const hours = Math.floor(totalMinutes / 60);
    const remainingMinutes = totalMinutes % 60;

    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }

  /**
   * Extracts the time window from a lecture item.
   * @param {Object} item - The lecture item.
   * @returns {Object|null} Object with start and end Date, or null if invalid.
   */
  function getLectureTimeWindow(item) {
    const startRaw = getItemProperty(item, 'startTime', 'StartTime');
    const endRaw = getItemProperty(item, 'endTime', 'EndTime');
    const duration = getItemProperty(item, 'duration', 'Duration');

    if (!startRaw) {
      return null;
    }

    const start = new Date(startRaw);
    if (Number.isNaN(start.getTime())) {
      return null;
    }

    // Try explicit end time first
    if (endRaw) {
      const end = new Date(endRaw);
      if (!Number.isNaN(end.getTime())) {
        return { start, end };
      }
    }

    // Calculate end from duration
    const minutes = parseDurationToMinutes(duration);
    const end = new Date(start.getTime() + (minutes * MS_PER_MINUTE));
    return { start, end };
  }

  // ============================================================================
  // UI State Functions
  // ============================================================================

  /**
   * Shows the join lecture UI state.
   */
  function showJoinState() {
    elements.joinState.hidden = false;
    elements.inClassState.hidden = true;

    state.activeLectureWindow = null;

    if (state.lectureProgressTimer) {
      clearInterval(state.lectureProgressTimer);
      state.lectureProgressTimer = null;
    }
  }

  /**
   * Renders the lecture progress bar and time information.
   */
  function renderLectureProgress() {
    const { track, fill, nowLine, timeRange, remaining } = elements;

    if (!state.activeLectureWindow || !track || !fill || !nowLine || !timeRange || !remaining) {
      return;
    }

    const { start, end } = state.activeLectureWindow;
    const now = new Date();

    const totalMs = Math.max(1, end.getTime() - start.getTime());
    const elapsedMs = now.getTime() - start.getTime();
    const clampedMs = Math.max(0, Math.min(totalMs, elapsedMs));
    const progressRatio = clampedMs / totalMs;

    // Update time range label
    timeRange.textContent = formatTimeRange(start, end);

    // Update remaining time label
    let statusText = '';
    if (now < start) {
      const minsToStart = (start.getTime() - now.getTime()) / MS_PER_MINUTE;
      statusText = `Starts in ${humanizeMinutes(minsToStart)}`;
    } else if (now >= end) {
      statusText = 'Ended';
    } else {
      const minsLeft = (end.getTime() - now.getTime()) / MS_PER_MINUTE;
      statusText = `${humanizeMinutes(minsLeft)} left`;
    }

    remaining.textContent = statusText;
    safeSetAttribute(track, 'aria-valuetext', statusText);

    // Update progress bar position
    const progressPercent = Math.max(0, Math.min(100, progressRatio * 100));
    nowLine.style.left = `${progressPercent}%`;
    fill.style.width = `${progressPercent}%`;

    safeSetAttribute(track, 'aria-valuenow', String(Math.round(progressPercent)));
  }

  /**
   * Shows the in-class UI state with lecture information.
   * @param {Object} item - The active lecture item.
   */
  function showInClassState(item) {
    const name = getItemProperty(item, 'name', 'Name', '');
    const description = getItemProperty(item, 'description', 'Description', '');

    elements.className.textContent = name;
    elements.classDesc.textContent = description;

    state.activeLectureWindow = getLectureTimeWindow(item);

    const { track, fill, nowLine, timeRange, remaining } = elements;
    const hasProgressElements = track && fill && nowLine && timeRange && remaining;

    if (hasProgressElements) {
      if (state.activeLectureWindow) {
        track.hidden = false;
        renderLectureProgress();

        if (!state.lectureProgressTimer) {
          state.lectureProgressTimer = setInterval(renderLectureProgress, PROGRESS_UPDATE_INTERVAL_MS);
        }
      } else {
        // Clear timing UI if we can't compute it
        timeRange.textContent = '';
        remaining.textContent = '';
        track.hidden = true;

        if (state.lectureProgressTimer) {
          clearInterval(state.lectureProgressTimer);
          state.lectureProgressTimer = null;
        }
      }
    }

    elements.joinState.hidden = true;
    elements.inClassState.hidden = false;
  }

  // ============================================================================
  // API Functions
  // ============================================================================

  /**
   * Builds the query string for the lectures API.
   * @param {Object} options - The query options.
   * @returns {string} The query string.
   */
  function buildLecturesQueryString({ page = 0, pageSize = 200, fromMonthsAgo = null, status = null } = {}) {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));

    if (fromMonthsAgo != null) {
      params.set('fromMonthsAgo', String(fromMonthsAgo));
    }

    if (status != null) {
      params.set('status', String(status));
    }

    return params.toString();
  }

  /**
   * Fetches student lectures from the API.
   * Tries multiple endpoint candidates for compatibility.
   * @param {Object} options - The fetch options.
   * @returns {Promise<Array|null>} The lectures array or null on failure.
   */
  async function fetchStudentLectures(options = {}) {
    const queryString = buildLecturesQueryString(options);
    let lastNon404Response = null;

    for (const baseUrl of API_ENDPOINTS) {
      const url = `${baseUrl}?${queryString}`;

      try {
        const response = await fetch(url, {
          method: 'GET',
          credentials: 'same-origin',
          headers: { 'Accept': 'application/json' }
        });

        if (response.status === 404) {
          continue;
        }

        lastNon404Response = response;

        if (!response.ok) {
          return null;
        }

        const data = await response.json();
        return Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      } catch {
        // Try next endpoint candidate
      }
    }

    if (lastNon404Response && !lastNon404Response.ok) {
      return null;
    }

    return null;
  }

  /**
   * Loads student lectures into the calendar.
   * @param {Object} options - The fetch options.
   */
  async function loadStudentLecturesToCalendar(options = {}) {
    const data = await fetchStudentLectures(options);

    if (!data || data.length === 0) {
      return;
    }

    // Check for active lecture and switch UI if found
    try {
      const activeLecture = data.find(function (item) {
        return isActiveLectureStatus(getItemProperty(item, 'status', 'Status'));
      });

      if (activeLecture) {
        showInClassState(activeLecture);
      }
    } catch {
      // Ignore errors when checking for active lecture
    }

    // Add events to calendar if available
    if (!globalThis.AttendanceCalendar || typeof globalThis.AttendanceCalendar.addEvent !== 'function') {
      return;
    }

    for (const item of data) {
      addLectureToCalendar(item);
    }
  }

  /**
   * Adds a single lecture item to the calendar.
   * @param {Object} item - The lecture item.
   */
  function addLectureToCalendar(item) {
    const id = getItemProperty(item, 'id', 'Id');
    const name = getItemProperty(item, 'name', 'Name', 'Untitled');
    const startTime = getItemProperty(item, 'startTime', 'StartTime');
    const description = getItemProperty(item, 'description', 'Description', '');
    const duration = getItemProperty(item, 'duration', 'Duration');
    const status = getItemProperty(item, 'status', 'Status');

    // Update status map for calendar pills
    if (id != null && status != null) {
      try {
        globalThis._lectureStatusMap[String(id)] = status;
      } catch {
        // Ignore status map errors
      }
    }

    const minutes = parseDurationToMinutes(duration);

    try {
      const startDate = startTime ? new Date(startTime) : null;
      const endDate = startDate ? new Date(startDate.getTime() + (minutes * MS_PER_MINUTE)) : null;

      if (startDate && endDate) {
        globalThis.AttendanceCalendar.addEvent({
          id: id,
          title: name,
          description: description,
          start: startDate,
          end: endDate,
          status: status
        });
      }
    } catch (error) {
      console.error('[StudentHome] Failed to add lecture to calendar:', error, item);
    }
  }

  /**
   * Detects and shows active lecture, or falls back to join state.
   */
  async function detectAndShowActiveLecture() {
    const data = await fetchStudentLectures({ page: 0, pageSize: 20, status: 'InProgress' });

    const activeLecture = Array.isArray(data)
      ? data.find(function (item) {
          return isActiveLectureStatus(getItemProperty(item, 'status', 'Status'));
        })
      : null;

    if (activeLecture) {
      showInClassState(activeLecture);
    } else {
      showJoinState();
    }
  }

  // ============================================================================
  // Navigation Functions
  // ============================================================================

  /**
   * Navigates to the lecture join page with the entered ID.
   */
  function navigateToJoinPage() {
    const id = (elements.classIdInput?.value ?? '').trim();

    if (!id) {
      try {
        elements.classIdInput?.focus();
      } catch {
        // Focus may fail
      }
      return;
    }

    globalThis.location.href = `/lecture/join/${encodeURIComponent(id)}`;
  }

  // ============================================================================
  // Calendar Integration
  // ============================================================================

  /**
   * Calculates the month difference from now.
   * @param {Date} now - The current date.
   * @param {Date} target - The target date.
   * @returns {number} The number of months ago (can be negative for future).
   */
  function calculateMonthsAgo(now, target) {
    return (now.getFullYear() * 12 + now.getMonth()) - (target.getFullYear() * 12 + target.getMonth());
  }

  /**
   * Creates a month key for tracking loaded months.
   * @param {Date} date - The date.
   * @returns {string} The month key (e.g., "2026-2").
   */
  function createMonthKey(date) {
    return `${date.getFullYear()}-${date.getMonth() + 1}`;
  }

  /**
   * Loads lectures for a specific month into the calendar.
   * @param {Date} monthDate - A date within the target month.
   * @param {Date} now - The current date for calculating months ago.
   */
  async function loadMonthLectures(monthDate, now) {
    const calendar = globalThis.AttendanceCalendar;
    const monthKey = createMonthKey(monthDate);

    if (typeof calendar.isMonthLoaded === 'function' && calendar.isMonthLoaded(monthKey)) {
      return;
    }

    if (typeof calendar.markMonthLoading === 'function') {
      calendar.markMonthLoading(monthKey);
    }

    const monthsAgo = calculateMonthsAgo(now, monthDate);
    await loadStudentLecturesToCalendar({ fromMonthsAgo: monthsAgo });

    if (typeof calendar.markMonthLoaded === 'function') {
      calendar.markMonthLoaded(monthKey);
    }
  }

  /**
   * Handles calendar view change events.
   * @param {string} monthKey - The month key.
   * @param {Date} viewDate - The view date.
   */
  async function handleCalendarViewChange(monthKey, viewDate) {
    const calendar = globalThis.AttendanceCalendar;

    try {
      if (typeof calendar.isMonthLoaded === 'function' && calendar.isMonthLoaded(monthKey)) {
        return;
      }

      if (typeof calendar.markMonthLoading === 'function') {
        calendar.markMonthLoading(monthKey);
      }

      const now = new Date();
      const monthsAgo = calculateMonthsAgo(now, viewDate);
      await loadStudentLecturesToCalendar({ fromMonthsAgo: monthsAgo });

      if (typeof calendar.markMonthLoaded === 'function') {
        calendar.markMonthLoaded(monthKey);
      }
    } catch (error) {
      console.error('[StudentHome] Calendar view change error:', error);
    }
  }

  /**
   * Performs initial calendar data load for surrounding months.
   */
  async function performInitialCalendarLoad() {
    try {
      const now = new Date();
      const currentMonth = new Date(now.getFullYear(), now.getMonth(), 1);
      const previousMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1);
      const nextMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1);

      const months = [previousMonth, currentMonth, nextMonth];

      for (const monthDate of months) {
        await loadMonthLectures(monthDate, now);
      }
    } catch (error) {
      console.error('[StudentHome] Initial calendar load error:', error);
    }
  }

  /**
   * Initializes calendar integration with polling for availability.
   */
  function initCalendarIntegration() {
    let checkCount = 0;

    const checkInterval = setInterval(function () {
      checkCount++;

      if (globalThis.AttendanceCalendar && typeof globalThis.AttendanceCalendar.onViewChange === 'function') {
        clearInterval(checkInterval);

        // Register view change handler
        globalThis.AttendanceCalendar.onViewChange(handleCalendarViewChange);

        // Perform initial load
        performInitialCalendarLoad();
      }

      if (checkCount >= CALENDAR_MAX_CHECKS) {
        clearInterval(checkInterval);
      }
    }, CALENDAR_CHECK_INTERVAL_MS);
  }

  // ============================================================================
  // Event Listeners
  // ============================================================================

  /**
   * Initializes event listeners.
   */
  function initEventListeners() {
    if (elements.joinBtn) {
      elements.joinBtn.addEventListener('click', function (event) {
        if (event) {
          event.preventDefault();
        }
        navigateToJoinPage();
      });
    }

    if (elements.classIdInput) {
      elements.classIdInput.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
          event.preventDefault();
          navigateToJoinPage();
        }
      });
    }
  }

  // ============================================================================
  // Initialization
  // ============================================================================

  /**
   * Initializes the student home page.
   */
  function init() {
    // Hide both states initially
    elements.joinState.hidden = true;
    elements.inClassState.hidden = true;

    // Set up event listeners
    initEventListeners();

    // Initialize calendar integration
    initCalendarIntegration();

    // Detect active lecture and show appropriate UI
    detectAndShowActiveLecture().catch(function () {
      showJoinState();
    });
  }

  init();
})();
