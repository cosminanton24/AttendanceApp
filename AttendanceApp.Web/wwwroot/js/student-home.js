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

  /** Quiz timer update interval in milliseconds */
  const QUIZ_TIMER_UPDATE_INTERVAL_MS = 1000;

  /** Warning threshold for quiz timer (5 minutes in ms) */
  const QUIZ_WARNING_THRESHOLD_MS = 5 * 60 * 1000;

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
    activeLectureWindow: null,
    activeLectureId: null,
    activeQuiz: null,
    quizTimer: null,
    currentQuestionIndex: 0,
    selectedAnswers: {} // Map of questionId -> optionId (single) or Set of optionIds (multiple)
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
    progress: document.getElementById('studentLectureProgress'),
    track: document.getElementById('studentLectureProgressTrack'),
    fill: document.getElementById('studentLectureProgressFill'),
    nowLine: document.getElementById('studentLectureNowLine'),
    joinBtn: document.getElementById('joinClassBtn'),
    classIdInput: document.getElementById('classIdInput'),
    // Quiz elements
    quizSection: document.getElementById('activeQuizSection'),
    quizName: document.getElementById('quizName'),
    quizEndTime: document.getElementById('quizEndTime'),
    quizTimerCircle: document.getElementById('quizTimerCircle'),
    quizTimeLeft: document.getElementById('quizTimeLeft'),
    quizQuestionNumber: document.getElementById('quizQuestionNumber'),
    quizQuestionPoints: document.getElementById('quizQuestionPoints'),
    quizQuestionText: document.getElementById('quizQuestionText'),
    quizOptions: document.getElementById('quizOptions'),
    quizPrevBtn: document.getElementById('quizPrevBtn'),
    quizNextBtn: document.getElementById('quizNextBtn'),
    quizQuestionDots: document.getElementById('quizQuestionDots'),
    quizSubmitBtn: document.getElementById('quizSubmitBtn'),
    quizSubmitModal: document.getElementById('quizSubmitModal'),
    quizSubmitUnanswered: document.getElementById('quizSubmitUnanswered'),
    quizConfirmSubmitBtn: document.getElementById('quizConfirmSubmitBtn')
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
    state.activeLectureId = null;

    if (state.lectureProgressTimer) {
      clearInterval(state.lectureProgressTimer);
      state.lectureProgressTimer = null;
    }

    // Hide quiz section when not in class
    if (elements.quizSection) {
      elements.quizSection.hidden = true;
    }
    if (state.quizTimer) {
      clearInterval(state.quizTimer);
      state.quizTimer = null;
    }
    state.activeQuiz = null;
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
    // update accessible progress element (hidden) with textual status
    safeSetAttribute(elements.progress, 'aria-valuetext', statusText);

    // Update progress bar position
    const progressPercent = Math.max(0, Math.min(100, progressRatio * 100));
    nowLine.style.left = `${progressPercent}%`;
    fill.style.width = `${progressPercent}%`;

    // update the <progress> value for assistive tech
    if (elements.progress) {
      try {
        elements.progress.value = Math.round(progressPercent);
      } catch {}
      safeSetAttribute(elements.progress, 'aria-valuenow', String(Math.round(progressPercent)));
    }
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

    // Check for active quiz after showing in-class state
    const lectureId = getItemProperty(item, 'id', 'Id', null);
    if (lectureId) {
      state.activeLectureId = lectureId;
      fetchAndDisplayActiveQuiz(lectureId);
    }
  }

  // ============================================================================
  // Quiz Functions
  // ============================================================================

  /**
   * Escapes HTML special characters.
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
   * Parses a duration string to milliseconds.
   * @param {string|null} duration - The duration string.
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
   * Formats time in mm:ss or hh:mm:ss format.
   * @param {number} ms - Milliseconds.
   * @returns {string} Formatted time string.
   */
  function formatTimeCompact(ms) {
    if (ms <= 0) {
      return '0:00';
    }

    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const mins = Math.floor((totalSeconds % 3600) / 60);
    const secs = totalSeconds % 60;

    if (hours > 0) {
      return `${hours}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    }
    return `${mins}:${String(secs).padStart(2, '0')}`;
  }

  /**
   * Fetches the active quiz for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {Promise<Object|null>} The active quiz or null.
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
   * Fetches user's saved answers for a quiz lecture.
   * @param {string} quizLectureId - The quiz lecture ID.
   * @returns {Promise<Array>} The user answers array.
   */
  async function fetchUserAnswers(quizLectureId) {
    try {
      const response = await fetch(`/api/quizzes/quiz-lecture/${encodeURIComponent(String(quizLectureId))}/answers`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return [];
      }

      const data = await response.json();
      return Array.isArray(data) ? data : [];
    } catch {
      return [];
    }
  }

  /**
   * Fetches user's submission for a quiz lecture.
   * @param {string} quizLectureId - The quiz lecture ID.
   * @returns {Promise<Object|null>} The submission or null.
   */
  async function fetchUserSubmission(quizLectureId) {
    try {
      const response = await fetch(`/api/quizzes/quiz-lecture/${encodeURIComponent(String(quizLectureId))}/submission`, {
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
   * Saves a user answer to the server.
   * @param {string} quizLectureId - The quiz lecture ID.
   * @param {string} questionId - The question ID.
   * @param {string} optionId - The option ID.
   * @param {boolean} choice - Whether the option is selected.
   * @returns {Promise<boolean>} True if saved successfully.
   */
  async function saveUserAnswer(quizLectureId, questionId, optionId, choice) {
    try {
      const response = await fetch('/api/quizzes/answer', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({
          quizLectureId: quizLectureId,
          questionId: questionId,
          optionId: optionId,
          choice: choice
        })
      });

      return response.ok;
    } catch {
      return false;
    }
  }

  /**
   * Clears the quiz timer.
   */
  function clearQuizTimer() {
    if (state.quizTimer) {
      clearInterval(state.quizTimer);
      state.quizTimer = null;
    }
  }

  /**
   * Hides the quiz section.
   */
  function hideQuizSection() {
    if (elements.quizSection) {
      elements.quizSection.hidden = true;
    }
    clearQuizTimer();
    state.activeQuiz = null;
    state.currentQuestionIndex = 0;
    state.selectedAnswers = {};
  }

  /**
   * Updates the circular timer display.
   */
  function updateQuizTimer() {
    const quiz = state.activeQuiz;
    if (!quiz) {
      hideQuizSection();
      return;
    }

    const activatedAt = quiz.activatedAtUtc ?? quiz.ActivatedAtUtc ?? quiz.activatedAt ?? quiz.ActivatedAt;
    const endTimeUtc = quiz.endTimeUtc ?? quiz.EndTimeUtc;
    const duration = quiz.duration ?? quiz.Duration;

    if (!activatedAt) {
      hideQuizSection();
      return;
    }

    const startTime = new Date(activatedAt);
    let endTime;
    let totalMs;

    if (endTimeUtc) {
      endTime = new Date(endTimeUtc);
      totalMs = Math.max(1, endTime.getTime() - startTime.getTime());
    } else if (duration) {
      const durationMs = parseDurationToMs(duration);
      totalMs = Math.max(1, durationMs);
      endTime = new Date(startTime.getTime() + totalMs);
    } else {
      hideQuizSection();
      return;
    }

    const now = new Date();
    const remainingMs = endTime.getTime() - now.getTime();

    // Quiz ended
    if (remainingMs <= 0) {
      hideQuizSection();
      return;
    }

    const elapsedMs = now.getTime() - startTime.getTime();
    const progressRatio = Math.max(0, Math.min(1, elapsedMs / totalMs));

    // Update circular timer (SVG stroke-dashoffset)
    // Circle circumference is 2 * PI * 45 ≈ 283
    const circumference = 283;
    const offset = circumference * progressRatio;

    if (elements.quizTimerCircle) {
      elements.quizTimerCircle.style.strokeDashoffset = String(offset);

      // Add warning class when < 5 minutes
      if (remainingMs < QUIZ_WARNING_THRESHOLD_MS) {
        elements.quizTimerCircle.classList.add('warning');
      } else {
        elements.quizTimerCircle.classList.remove('warning');
      }
    }

    if (elements.quizTimeLeft) {
      elements.quizTimeLeft.textContent = formatTimeCompact(remainingMs);

      if (remainingMs < QUIZ_WARNING_THRESHOLD_MS) {
        elements.quizTimeLeft.classList.add('warning');
      } else {
        elements.quizTimeLeft.classList.remove('warning');
      }
    }
  }

  /**
   * Checks if an option is selected for a question.
   * @param {string} questionId - The question ID.
   * @param {string} optionId - The option ID.
   * @returns {boolean} True if selected.
   */
  function isOptionSelected(questionId, optionId) {
    const selected = state.selectedAnswers[questionId];
    if (!selected) {
      return false;
    }
    if (selected instanceof Set) {
      return selected.has(optionId);
    }
    return selected === optionId;
  }

  /**
   * Checks if a question has any answer selected.
   * @param {string} questionId - The question ID.
   * @returns {boolean} True if answered.
   */
  function isQuestionAnswered(questionId) {
    const selected = state.selectedAnswers[questionId];
    if (!selected) {
      return false;
    }
    if (selected instanceof Set) {
      return selected.size > 0;
    }
    return true;
  }

  /**
   * Counts correct options for a question to determine if multiple choice.
   * @param {Object} question - The question object.
   * @returns {number} Number of correct options.
   */
  function countCorrectOptions(question) {
    const options = question?.options ?? question?.Options ?? [];
    return options.filter(function (opt) {
      return opt?.isCorrect ?? opt?.IsCorrect ?? false;
    }).length;
  }

  /**
   * Renders the current question.
   */
  function renderCurrentQuestion() {
    const quiz = state.activeQuiz;
    if (!quiz) {
      return;
    }

    const questions = quiz.questions ?? quiz.Questions ?? [];
    if (questions.length === 0) {
      if (elements.quizQuestionText) {
        elements.quizQuestionText.textContent = 'No questions available.';
      }
      return;
    }

    const index = Math.max(0, Math.min(state.currentQuestionIndex, questions.length - 1));
    const question = questions[index];
    const questionId = question?.id ?? question?.Id;

    // Update question number
    if (elements.quizQuestionNumber) {
      elements.quizQuestionNumber.textContent = `Question ${index + 1} of ${questions.length}`;
    }

    // Update points
    if (elements.quizQuestionPoints) {
      const points = question?.points ?? question?.Points ?? 0;
      elements.quizQuestionPoints.textContent = points === 1 ? '1 point' : `${points} points`;
    }

    // Update question text
    if (elements.quizQuestionText) {
      elements.quizQuestionText.textContent = question?.text ?? question?.Text ?? '';
    }

    // Render options
    if (elements.quizOptions) {
      const options = question?.options ?? question?.Options ?? [];
      const optionLetters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];

      elements.quizOptions.innerHTML = options.map(function (opt, i) {
        const optId = opt?.id ?? opt?.Id;
        const optText = opt?.text ?? opt?.Text ?? '';
        const isSelected = isOptionSelected(questionId, optId);
        const letter = optionLetters[i] || String(i + 1);

        return `
          <div class="student-quiz__option${isSelected ? ' selected' : ''}" data-option-id="${escapeHtml(optId)}" data-question-id="${escapeHtml(questionId)}">
            <div class="student-quiz__option-marker">${isSelected ? '✓' : letter}</div>
            <div class="student-quiz__option-text">${escapeHtml(optText)}</div>
          </div>
        `;
      }).join('');

      // Wire up option click handlers - all options are toggleable (multiple choice)
      elements.quizOptions.querySelectorAll('.student-quiz__option').forEach(function (optEl) {
        optEl.addEventListener('click', function () {
          const optionId = optEl.dataset.optionId;
          const qId = optEl.dataset.questionId;

          // Toggle selection (always multiple choice behavior)
          let selected = state.selectedAnswers[qId];
          if (!(selected instanceof Set)) {
            selected = new Set();
            state.selectedAnswers[qId] = selected;
          }

          const isNowSelected = !selected.has(optionId);
          if (isNowSelected) {
            selected.add(optionId);
          } else {
            selected.delete(optionId);
          }

          // Re-render to update checkmarks
          renderCurrentQuestion();

          // Save answer to server (fire and forget)
          const quizLectureId = state.activeQuiz?.quizLectureId ?? state.activeQuiz?.QuizLectureId;
          if (quizLectureId) {
            saveUserAnswer(quizLectureId, qId, optionId, isNowSelected);
          }
        });
      });
    }

    // Update navigation buttons
    if (elements.quizPrevBtn) {
      elements.quizPrevBtn.disabled = index === 0;
    }
    if (elements.quizNextBtn) {
      elements.quizNextBtn.disabled = index >= questions.length - 1;
    }

    // Render question dots
    renderQuestionDots();
  }

  /**
   * Renders the question navigation dots.
   */
  function renderQuestionDots() {
    const quiz = state.activeQuiz;
    if (!quiz || !elements.quizQuestionDots) {
      return;
    }

    const questions = quiz.questions ?? quiz.Questions ?? [];
    const currentIndex = state.currentQuestionIndex;

    elements.quizQuestionDots.innerHTML = questions.map(function (q, i) {
      const qId = q?.id ?? q?.Id;
      const isAnswered = isQuestionAnswered(qId);
      const isActive = i === currentIndex;

      let classes = 'student-quiz__dot';
      if (isActive) {
        classes += ' active';
      }
      if (isAnswered) {
        classes += ' answered';
      }

      return `<div class="${classes}" data-question-index="${i}"></div>`;
    }).join('');

    // Wire up dot click handlers
    elements.quizQuestionDots.querySelectorAll('.student-quiz__dot').forEach(function (dot) {
      dot.addEventListener('click', function () {
        const idx = Number.parseInt(dot.dataset.questionIndex, 10);
        if (!Number.isNaN(idx)) {
          state.currentQuestionIndex = idx;
          renderCurrentQuestion();
        }
      });
    });
  }

  /**
   * Displays the active quiz.
   * @param {Object} quiz - The quiz data.
   */
  async function displayActiveQuiz(quiz) {
    if (!quiz || !elements.quizSection) {
      hideQuizSection();
      return;
    }

    state.activeQuiz = quiz;
    state.currentQuestionIndex = 0;
    state.selectedAnswers = {};

    const quizLectureId = quiz.quizLectureId ?? quiz.QuizLectureId;

    // Check if user already submitted this quiz
    if (quizLectureId) {
      try {
        const submission = await fetchUserSubmission(quizLectureId);
        if (submission && submission.submitted) {
          // Show result view directly
          elements.quizSection.hidden = false;
          displayQuizResultFromSubmission(quiz, submission);
          return;
        }
      } catch (err) {
        console.error('[StudentHome] Failed to check submission:', err);
      }
    }

    // Load saved answers if any
    if (quizLectureId) {
      try {
        const savedAnswers = await fetchUserAnswers(quizLectureId);
        // Populate selectedAnswers from saved answers (only those with choice = true)
        savedAnswers.forEach(function (answer) {
          const qId = answer.questionId ?? answer.QuestionId;
          const optId = answer.optionId ?? answer.OptionId;
          const choice = answer.choice ?? answer.Choice;

          if (choice) {
            if (!state.selectedAnswers[qId]) {
              state.selectedAnswers[qId] = new Set();
            }
            state.selectedAnswers[qId].add(optId);
          }
        });
      } catch (err) {
        console.error('[StudentHome] Failed to load saved answers:', err);
      }
    }

    // Show section
    elements.quizSection.hidden = false;

    // Set quiz name
    if (elements.quizName) {
      elements.quizName.textContent = quiz.name ?? quiz.Name ?? 'Quiz';
    }

    // Set end time
    if (elements.quizEndTime) {
      const endTimeUtc = quiz.endTimeUtc ?? quiz.EndTimeUtc;
      if (endTimeUtc) {
        try {
          const endTime = new Date(endTimeUtc);
          elements.quizEndTime.textContent = `Ends at ${endTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        } catch {
          elements.quizEndTime.textContent = '';
        }
      } else {
        elements.quizEndTime.textContent = '';
      }
    }

    // Initialize timer
    updateQuizTimer();
    clearQuizTimer();
    state.quizTimer = setInterval(updateQuizTimer, QUIZ_TIMER_UPDATE_INTERVAL_MS);

    // Render first question
    renderCurrentQuestion();

    // Wire up navigation buttons
    if (elements.quizPrevBtn) {
      elements.quizPrevBtn.onclick = function () {
        if (state.currentQuestionIndex > 0) {
          state.currentQuestionIndex--;
          renderCurrentQuestion();
        }
      };
    }

    if (elements.quizNextBtn) {
      elements.quizNextBtn.onclick = function () {
        const questions = state.activeQuiz?.questions ?? state.activeQuiz?.Questions ?? [];
        if (state.currentQuestionIndex < questions.length - 1) {
          state.currentQuestionIndex++;
          renderCurrentQuestion();
        }
      };
    }

    // Wire up submit button
    if (elements.quizSubmitBtn) {
      elements.quizSubmitBtn.onclick = function () {
        openQuizSubmitModal();
      };
    }
  }

  /**
   * Opens the quiz submit confirmation modal.
   */
  function openQuizSubmitModal() {
    if (!elements.quizSubmitModal) return;

    // Count unanswered questions
    const questions = state.activeQuiz?.questions ?? state.activeQuiz?.Questions ?? [];
    let unansweredCount = 0;

    questions.forEach(function (question) {
      const questionId = question?.id ?? question?.Id;
      const selected = state.selectedAnswers[questionId];
      
      // Check if no answer selected
      if (!selected || (selected instanceof Set && selected.size === 0)) {
        unansweredCount++;
      }
    });

    // Update warning message
    if (elements.quizSubmitUnanswered) {
      if (unansweredCount > 0) {
        elements.quizSubmitUnanswered.textContent = `You have ${unansweredCount} unanswered question${unansweredCount > 1 ? 's' : ''}.`;
        elements.quizSubmitUnanswered.hidden = false;
      } else {
        elements.quizSubmitUnanswered.textContent = '';
        elements.quizSubmitUnanswered.hidden = true;
      }
    }

    elements.quizSubmitModal.classList.add('open');
    elements.quizSubmitModal.setAttribute('aria-hidden', 'false');
  }

  /**
   * Closes the quiz submit confirmation modal.
   */
  function closeQuizSubmitModal() {
    if (!elements.quizSubmitModal) return;
    elements.quizSubmitModal.classList.remove('open');
    elements.quizSubmitModal.setAttribute('aria-hidden', 'true');
  }

  /**
   * Handles the quiz submission.
   */
  async function handleQuizSubmit() {
    const quizLectureId = state.activeQuiz?.quizLectureId ?? state.activeQuiz?.QuizLectureId;
    if (!quizLectureId) {
      console.error('[StudentHome] No quiz lecture ID found');
      closeQuizSubmitModal();
      return;
    }

    // Disable submit button while processing
    if (elements.quizConfirmSubmitBtn) {
      elements.quizConfirmSubmitBtn.disabled = true;
      elements.quizConfirmSubmitBtn.textContent = 'Submitting...';
    }

    try {
      const response = await fetch(`/api/quizzes/quiz-lecture/${encodeURIComponent(String(quizLectureId))}/submit`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        throw new Error('Failed to submit quiz');
      }

      const result = await response.json();
      closeQuizSubmitModal();
      displayQuizResult(result);
    } catch (error) {
      console.error('[StudentHome] Failed to submit quiz:', error);
      // Re-enable button on error
      if (elements.quizConfirmSubmitBtn) {
        elements.quizConfirmSubmitBtn.disabled = false;
        elements.quizConfirmSubmitBtn.textContent = 'Submit';
      }
    }
  }

  /**
   * Displays the quiz result after submission.
   * @param {Object} result - The quiz result from the API.
   */
  function displayQuizResult(result) {
    const score = result.score ?? result.Score ?? 0;
    const maxScore = result.maxScore ?? result.MaxScore ?? 0;
    const correctQuestions = result.correctQuestions ?? result.CorrectQuestions ?? 0;
    const totalQuestions = result.totalQuestions ?? result.TotalQuestions ?? 0;

    // Stop the timer
    clearQuizTimer();

    // Replace quiz content with result
    if (elements.quizSection) {
      const quizName = state.activeQuiz?.name ?? state.activeQuiz?.Name ?? 'Quiz';
      const percentage = maxScore > 0 ? Math.round((score / maxScore) * 100) : 0;
      const icon = percentage >= 70 ? '🎉' : percentage >= 50 ? '👍' : '📚';

      elements.quizSection.innerHTML = `
        <div class="student-quiz__header">
          <div class="student-quiz__info">
            <div class="student-quiz__title">Quiz Completed</div>
            <div class="student-quiz__name">${escapeHtml(quizName)}</div>
          </div>
        </div>
        <div class="student-quiz__result">
          <div class="student-quiz__result-icon">${icon}</div>
          <div class="student-quiz__result-title">Your Score</div>
          <div class="student-quiz__result-score">${score} / ${maxScore}</div>
          <div class="student-quiz__result-details">
            ${correctQuestions} of ${totalQuestions} questions correct (${percentage}%)
          </div>
        </div>
      `;
    }

    // Clear state
    state.activeQuiz = null;
    state.selectedAnswers = {};
    state.currentQuestionIndex = 0;
  }

  /**
   * Displays the quiz result from a saved submission (on page reload).
   * @param {Object} quiz - The quiz data.
   * @param {Object} submission - The submission data.
   */
  function displayQuizResultFromSubmission(quiz, submission) {
    const score = submission.score ?? submission.Score ?? 0;
    const maxScore = submission.maxScore ?? submission.MaxScore ?? 0;
    const quizName = quiz.name ?? quiz.Name ?? 'Quiz';
    const percentage = maxScore > 0 ? Math.round((score / maxScore) * 100) : 0;
    const icon = percentage >= 70 ? '🎉' : percentage >= 50 ? '👍' : '📚';

    if (elements.quizSection) {
      elements.quizSection.innerHTML = `
        <div class="student-quiz__header">
          <div class="student-quiz__info">
            <div class="student-quiz__title">Quiz Completed</div>
            <div class="student-quiz__name">${escapeHtml(quizName)}</div>
          </div>
        </div>
        <div class="student-quiz__result">
          <div class="student-quiz__result-icon">${icon}</div>
          <div class="student-quiz__result-title">Your Score</div>
          <div class="student-quiz__result-score">${score} / ${maxScore}</div>
          <div class="student-quiz__result-details">
            (${percentage}%)
          </div>
        </div>
      `;
    }

    // Clear state
    state.activeQuiz = null;
    state.selectedAnswers = {};
    state.currentQuestionIndex = 0;
  }

  /**
   * Initializes the quiz submit modal event handlers.
   */
  function initQuizSubmitModal() {
    if (!elements.quizSubmitModal) return;

    // Close modal on backdrop click or close button click
    elements.quizSubmitModal.querySelectorAll('[data-close-quiz-modal]').forEach(function (el) {
      el.addEventListener('click', closeQuizSubmitModal);
    });

    // Confirm submit button
    if (elements.quizConfirmSubmitBtn) {
      elements.quizConfirmSubmitBtn.addEventListener('click', handleQuizSubmit);
    }

    // Close on Escape key
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && elements.quizSubmitModal.classList.contains('open')) {
        closeQuizSubmitModal();
      }
    });
  }

  /**
   * Fetches and displays the active quiz for a lecture.
   * @param {string} lectureId - The lecture ID.
   */
  async function fetchAndDisplayActiveQuiz(lectureId) {
    try {
      const quiz = await fetchActiveQuizForLecture(lectureId);

      if (quiz) {
        await displayActiveQuiz(quiz);
      } else {
        hideQuizSection();
      }
    } catch (error) {
      console.error('[StudentHome] Failed to fetch active quiz:', error);
      hideQuizSection();
    }
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
  // Lecture Popup (Calendar Click)
  // ============================================================================

  /**
   * Fetches student quiz results for a lecture.
   * @param {string} lectureId - The lecture ID.
   * @returns {Promise<Array>} The quiz results.
   */
  async function fetchStudentQuizResults(lectureId) {
    try {
      const response = await fetch(`/api/quizzes/lecture/${lectureId}/student-results`, {
        method: 'GET',
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json' }
      });

      if (!response.ok) {
        return [];
      }

      const data = await response.json();
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error('[StudentHome] Failed to fetch student quiz results:', error);
      return [];
    }
  }

  /**
   * Creates the lecture popup element if it doesn't exist.
   * @returns {HTMLElement} The popup element.
   */
  function ensureLecturePopup() {
    let popup = document.getElementById('studentLecturePopup');

    if (!popup) {
      popup = document.createElement('div');
      popup.id = 'studentLecturePopup';
      popup.className = 'student-lecture-popup';
      popup.innerHTML = `
        <button type="button" class="slp-close" aria-label="Close">×</button>
        <div class="slp-body">
          <div class="slp-title"></div>
          <div class="slp-time small text-muted"></div>
          <div class="slp-quizzes-section">
            <div class="slp-quizzes-title">Quizzes</div>
            <div class="slp-quizzes-list"></div>
            <div class="slp-quizzes-empty">No quizzes for this lecture.</div>
          </div>
        </div>
      `;
      document.body.appendChild(popup);

      // Close on clicking the close button
      const closeBtn = popup.querySelector('.slp-close');
      if (closeBtn) {
        closeBtn.addEventListener('click', function (event) {
          event.stopPropagation();
          popup.style.display = 'none';
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
   * Renders the quiz list in the popup.
   * @param {HTMLElement} popup - The popup element.
   * @param {Array} quizzes - The quiz results array.
   */
  function renderQuizList(popup, quizzes) {
    const listEl = popup.querySelector('.slp-quizzes-list');
    const emptyEl = popup.querySelector('.slp-quizzes-empty');

    if (!listEl || !emptyEl) return;

    listEl.innerHTML = '';

    if (!quizzes || quizzes.length === 0) {
      listEl.style.display = 'none';
      emptyEl.style.display = 'block';
      return;
    }

    listEl.style.display = 'block';
    emptyEl.style.display = 'none';

    quizzes.forEach(function (quiz) {
      const quizName = quiz.quizName ?? quiz.QuizName ?? 'Quiz';
      const hasSubmitted = quiz.hasSubmitted ?? quiz.HasSubmitted ?? false;
      const score = quiz.score ?? quiz.Score;
      const maxScore = quiz.maxScore ?? quiz.MaxScore;

      const item = document.createElement('div');
      item.className = 'slp-quiz-item';

      const nameSpan = document.createElement('span');
      nameSpan.className = 'slp-quiz-name';
      nameSpan.textContent = quizName;

      const scoreSpan = document.createElement('span');
      scoreSpan.className = 'slp-quiz-score';

      if (hasSubmitted && score != null && maxScore != null) {
        scoreSpan.textContent = `${score} / ${maxScore}`;
        scoreSpan.classList.add('slp-quiz-score--submitted');
      } else if (hasSubmitted) {
        scoreSpan.textContent = 'Submitted';
        scoreSpan.classList.add('slp-quiz-score--submitted');
      } else {
        scoreSpan.textContent = 'Not submitted';
        scoreSpan.classList.add('slp-quiz-score--pending');
      }

      item.appendChild(nameSpan);
      item.appendChild(scoreSpan);
      listEl.appendChild(item);
    });
  }

  /**
   * Handles calendar event clicks to show the lecture popup.
   * @param {Object} ev - The event object.
   */
  globalThis.onCalendarEventClick = async function (ev) {
    try {
      const eventEl = document.querySelector(`[data-event-id="${ev.id}"]`);
      const popup = ensureLecturePopup();

      const titleEl = popup.querySelector('.slp-title');
      const timeEl = popup.querySelector('.slp-time');

      if (titleEl) {
        titleEl.textContent = ev.title || '';
      }

      if (timeEl) {
        const startStr = new Date(ev.start).toLocaleString();
        const endStr = new Date(ev.end).toLocaleString();
        timeEl.textContent = `${startStr} — ${endStr}`;
      }

      // Show loading state
      const listEl = popup.querySelector('.slp-quizzes-list');
      const emptyEl = popup.querySelector('.slp-quizzes-empty');
      if (listEl) listEl.innerHTML = '<div class="slp-loading">Loading...</div>';
      if (listEl) listEl.style.display = 'block';
      if (emptyEl) emptyEl.style.display = 'none';

      positionLecturePopup(popup, eventEl);

      // Fetch quiz results
      const quizzes = await fetchStudentQuizResults(ev.id);
      renderQuizList(popup, quizzes);

      // Re-position after content load
      positionLecturePopup(popup, eventEl);
    } catch (error) {
      console.error('[StudentHome] Calendar event click error:', error);
    }
  };

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

    // Initialize quiz submit modal
    initQuizSubmitModal();

    // Detect active lecture and show appropriate UI
    detectAndShowActiveLecture().catch(function () {
      showJoinState();
    });
  }

  init();
})();
