/**
 * @fileoverview Professor Quizzes - popover list + modal editor with validation.
 */

'use strict';

(function () {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  function init() {
    // ========================================================================
    // Constants
    // ========================================================================
    const PAGE_SIZE = 10;
    const SEARCH_DEBOUNCE_MS = 300;
    const SAVE_DEBOUNCE_MS = 600;

    const API = Object.freeze({
      MY_QUIZZES: '/api/quizzes/me',
      QUIZZES: '/api/quizzes',
      QUESTIONS: '/api/quizzes/questions',
      OPTIONS: '/api/quizzes/options'
    });

    // ========================================================================
    // State
    // ========================================================================
    const state = {
      currentPage: 0,
      totalCount: 0,
      loadedQuizzes: [],
      isLoading: false,
      searchQuery: '',
      searchTimeout: null,
      popoverOpen: false,
      currentQuiz: null,
      saveTimeouts: new Map()
    };

    // ========================================================================
    // DOM Elements
    // ========================================================================
    const el = {
      // Popover
      popover: document.getElementById('quizzesPopover'),
      quizzesBtn: document.getElementById('quizzesBtn'),
      searchInput: document.getElementById('quizSearchInput'),
      quizList: document.getElementById('quizList'),
      createBtn: document.getElementById('createQuizBtn'),
      // Create modal
      createModal: document.getElementById('createQuizModal'),
      createForm: document.getElementById('createQuizForm'),
      createFeedback: document.getElementById('createQuizFeedback'),
      // Edit modal
      editModal: document.getElementById('editQuizModal'),
      editName: document.getElementById('editQuizName'),
      editDuration: document.getElementById('editQuizDuration'),
      editBody: document.getElementById('editQuizBody'),
      editWarnings: document.getElementById('editQuizWarnings'),
      addQuestionBtn: document.getElementById('addQuestionBtn'),
      editCloseBtn: document.getElementById('editQuizCloseBtn'),
      editDoneBtn: document.getElementById('editQuizDoneBtn')
    };

    if (!el.popover || !el.quizList) return;

    // ========================================================================
    // Utilities
    // ========================================================================
    function escapeHtml(str) {
      return String(str ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
    }

    function formatDuration(duration) {
      if (!duration) return '';
      const parts = duration.split(':');
      let hours = 0, minutes = 0;
      if (parts.length >= 2) {
        if (parts[0].includes('.')) {
          const dayHour = parts[0].split('.');
          hours = Number.parseInt(dayHour[0], 10) * 24 + Number.parseInt(dayHour[1], 10);
        } else {
          hours = Number.parseInt(parts[0], 10);
        }
        minutes = Number.parseInt(parts[1], 10);
      }
      if (hours > 0 && minutes > 0) return `${hours}h ${minutes}m`;
      if (hours > 0) return `${hours}h`;
      if (minutes > 0) return `${minutes}m`;
      return '0m';
    }

    function parseDurationInput(input) {
      if (!input) return null;
      const normalized = input.toLowerCase().replaceAll(/\s+/g, '');
      let totalMinutes = 0;
      const hourMatch = /(\d+)h/.exec(normalized);
      const minMatch = /(\d+)m/.exec(normalized);
      if (hourMatch) totalMinutes += Number.parseInt(hourMatch[1], 10) * 60;
      if (minMatch) totalMinutes += Number.parseInt(minMatch[1], 10);
      if (!hourMatch && !minMatch) {
        const numMatch = /^(\d+)$/.exec(normalized);
        if (numMatch) totalMinutes = Number.parseInt(numMatch[1], 10);
      }
      if (totalMinutes <= 0) return null;
      const h = Math.floor(totalMinutes / 60);
      const m = totalMinutes % 60;
      return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:00`;
    }

    function debounce(fn, key, delay) {
      if (state.saveTimeouts.has(key)) {
        clearTimeout(state.saveTimeouts.get(key));
      }
      const timeout = setTimeout(() => {
        state.saveTimeouts.delete(key);
        fn();
      }, delay);
      state.saveTimeouts.set(key, timeout);
    }

    // ========================================================================
    // Popover
    // ========================================================================
    function openPopover() {
      const calPopover = document.getElementById('calendarPopover');
      if (calPopover?.classList.contains('open')) {
        calPopover.classList.remove('open');
        calPopover.setAttribute('aria-hidden', 'true');
      }
      el.popover.classList.add('open');
      el.popover.setAttribute('aria-hidden', 'false');
      state.popoverOpen = true;
      if (state.loadedQuizzes.length === 0 && !state.isLoading) {
        fetchQuizzes(0);
      }
      el.searchInput?.focus();
    }

    function closePopover() {
      el.popover.classList.remove('open');
      el.popover.setAttribute('aria-hidden', 'true');
      state.popoverOpen = false;
    }

    function togglePopover() {
      state.popoverOpen ? closePopover() : openPopover();
    }

    // ========================================================================
    // Quiz List
    // ========================================================================
    function renderQuizItem(quiz) {
      return `
        <div class="quiz-list-item" data-quiz-id="${escapeHtml(quiz.id)}">
          <div class="quiz-item-main">
            <div class="quiz-item-name">${escapeHtml(quiz.name)}</div>
            <div class="quiz-item-meta">
              <span> ${escapeHtml(formatDuration(quiz.duration))}</span>
              <span> ${quiz.questionsCount ?? 0} questions</span>
            </div>
          </div>
          <div class="quiz-item-actions">
            <button type="button" class="quiz-item-btn delete" data-delete-quiz="${escapeHtml(quiz.id)}" title="Delete">🗑</button>
          </div>
        </div>
      `;
    }

    function renderQuizzes() {
      if (state.isLoading) {
        el.quizList.innerHTML = '<div class="quiz-loading">Loading...</div>';
        return;
      }
      if (state.loadedQuizzes.length === 0) {
        el.quizList.innerHTML = '<div class="quiz-empty">No quizzes found</div>';
        return;
      }
      let html = state.loadedQuizzes.map(renderQuizItem).join('');
      if (state.loadedQuizzes.length < state.totalCount) {
        html += `
          <button type="button" class="quiz-load-more" id="quizLoadMore">
            <span class="plus-icon">+</span>
            <span>Load more (${state.loadedQuizzes.length}/${state.totalCount})</span>
          </button>
        `;
      }
      el.quizList.innerHTML = html;
    }

    async function fetchQuizzes(page) {
      state.isLoading = true;
      if (page === 0) {
        state.loadedQuizzes = [];
        renderQuizzes();
      }
      try {
        const params = new URLSearchParams({ page: page.toString(), pageSize: PAGE_SIZE.toString() });
        if (state.searchQuery) params.set('name', state.searchQuery);
        const res = await fetch(`${API.MY_QUIZZES}?${params}`, { credentials: 'include' });
        if (!res.ok) throw new Error('Failed to fetch');
        const data = await res.json();
        state.totalCount = data.total ?? 0;
        const items = data.items ?? [];
        state.loadedQuizzes = page === 0 ? items : [...state.loadedQuizzes, ...items];
        state.currentPage = page;
      } catch (err) {
        console.error('Error fetching quizzes:', err);
      } finally {
        state.isLoading = false;
        renderQuizzes();
      }
    }

    // ========================================================================
    // Create Modal
    // ========================================================================
    function openCreateModal() {
      el.createModal?.classList.add('open');
      el.createModal?.setAttribute('aria-hidden', 'false');
      document.getElementById('quizName')?.focus();
    }

    function closeCreateModal() {
      el.createModal?.classList.remove('open');
      el.createModal?.setAttribute('aria-hidden', 'true');
      el.createForm?.reset();
      if (el.createFeedback) {
        el.createFeedback.textContent = '';
        el.createFeedback.className = 'qe-feedback';
      }
    }

    function showCreateFeedback(message, type) {
      if (el.createFeedback) {
        el.createFeedback.textContent = message;
        el.createFeedback.className = `qe-feedback ${type || ''}`;
      }
    }

    async function createQuiz() {
      const nameInput = document.getElementById('quizName');
      const durationInput = document.getElementById('quizDuration');
      const name = nameInput?.value?.trim();
      const durationStr = parseDurationInput(durationInput?.value);

      if (!name) {
        showCreateFeedback('Please enter a quiz name', 'error');
        return;
      }
      if (!durationStr) {
        showCreateFeedback('Please enter a valid duration (e.g., 15m, 1h30m)', 'error');
        return;
      }

      try {
        const res = await fetch(API.QUIZZES, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ name, duration: durationStr })
        });
        if (!res.ok) throw new Error('Failed to create');
        showCreateFeedback('Quiz created!', 'success');
        closeCreateModal();
        fetchQuizzes(0);
      } catch (err) {
        console.error('Error creating quiz:', err);
        showCreateFeedback('Failed to create quiz', 'error');
      }
    }

    async function deleteQuiz(quizId) {
      if (!confirm('Delete this quiz?')) return;
      try {
        const res = await fetch(`${API.QUIZZES}/${quizId}`, { method: 'DELETE', credentials: 'include' });
        if (!res.ok) throw new Error('Failed to delete');
        state.loadedQuizzes = state.loadedQuizzes.filter(q => q.id !== quizId);
        state.totalCount = Math.max(0, state.totalCount - 1);
        renderQuizzes();
      } catch (err) {
        console.error('Error deleting quiz:', err);
      }
    }

    // ========================================================================
    // Edit Modal
    // ========================================================================
    async function openEditModal(quizId) {
      try {
        const res = await fetch(`${API.QUIZZES}/${quizId}`, { credentials: 'include' });
        if (!res.ok) throw new Error('Failed to load');
        state.currentQuiz = await res.json();
        renderEditModal();
        el.editModal?.classList.add('open');
        el.editModal?.setAttribute('aria-hidden', 'false');
      } catch (err) {
        console.error('Error loading quiz:', err);
      }
    }

    function closeEditModal() {
      el.editModal?.classList.remove('open');
      el.editModal?.setAttribute('aria-hidden', 'true');
      state.currentQuiz = null;
      // Refresh list to update question counts
      fetchQuizzes(0);
    }

    function renderEditModal() {
      if (!state.currentQuiz) return;
      el.editName.value = state.currentQuiz.name || '';
      el.editDuration.value = formatDuration(state.currentQuiz.duration);
      renderQuestions();
      renderWarnings();
    }

    function renderWarnings() {
      if (!el.editWarnings) return;
      const questions = state.currentQuiz?.questions || [];
      const warnings = [];

      if (questions.length === 0) {
        warnings.push('Quiz has no questions');
      }

      for (const q of questions) {
        const opts = q.options || [];
        const questionNum = questions.indexOf(q) + 1;
        const hasCorrect = opts.some(o => o.isCorrect);
        
        if (opts.length < 2) {
          warnings.push(`Question ${questionNum} has only ${opts.length} option${opts.length === 1 ? '' : 's'} (need at least 2)`);
        }
        if (opts.length > 0 && !hasCorrect) {
          warnings.push(`Question ${questionNum} has no correct answer marked`);
        }
      }

      if (warnings.length === 0) {
        el.editWarnings.innerHTML = '';
        return;
      }

      el.editWarnings.innerHTML = warnings.map(w => `
        <div class="quiz-editor-warning">
          <span class="quiz-editor-warning-icon">⚠</span>
          <span class="quiz-editor-warning-text">${escapeHtml(w)}</span>
        </div>
      `).join('');
    }

    function renderQuestions() {
      const questions = state.currentQuiz?.questions || [];
      if (questions.length === 0) {
        el.editBody.innerHTML = '<div class="qe-empty">No questions yet. Add one below!</div>';
        return;
      }

      let html = '';
      questions.forEach((q, idx) => {
        const opts = q.options || [];
        html += `
          <div class="qe-question" data-question-id="${escapeHtml(q.id)}">
            <div class="qe-question-header">
              <div class="qe-question-num">${idx + 1}</div>
              <input type="text" class="qe-question-input" value="${escapeHtml(q.text)}" 
                     placeholder="Question text..." data-field="text" />
              <input type="text" class="qe-question-points" value="${q.points ?? ''}" 
                     placeholder="pts" data-field="points" title="Points" />
              <button type="button" class="qe-question-delete" data-delete-question="${escapeHtml(q.id)}" title="Delete question">🗑</button>
            </div>
            <div class="qe-options">
              ${opts.map(o => `
                <div class="qe-option" data-option-id="${escapeHtml(o.id)}">
                  <button type="button" class="qe-option-correct ${o.isCorrect ? 'is-correct' : ''}" 
                          data-toggle-correct="${escapeHtml(o.id)}" title="Mark as correct"></button>
                  <input type="text" class="qe-option-input" value="${escapeHtml(o.text)}" 
                         placeholder="Option text..." data-field="text" />
                  <button type="button" class="qe-option-delete" data-delete-option="${escapeHtml(o.id)}" title="Delete">✕</button>
                </div>
              `).join('')}
              <button type="button" class="qe-add-option" data-add-option="${escapeHtml(q.id)}">
                <span class="plus-icon">+</span> Add option
              </button>
            </div>
          </div>
        `;
      });
      el.editBody.innerHTML = html;
    }

    // ========================================================================
    // API - Quiz Updates
    // ========================================================================
    async function updateQuizName(name) {
      if (!state.currentQuiz) return;
      try {
        await fetch(`${API.QUIZZES}/${state.currentQuiz.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ name })
        });
        state.currentQuiz.name = name;
        const listQuiz = state.loadedQuizzes.find(q => q.id === state.currentQuiz.id);
        if (listQuiz) listQuiz.name = name;
      } catch (err) {
        console.error('Error updating quiz name:', err);
      }
    }

    async function updateQuizDuration(durationStr) {
      if (!state.currentQuiz) return;
      try {
        await fetch(`${API.QUIZZES}/${state.currentQuiz.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ duration: durationStr })
        });
        state.currentQuiz.duration = durationStr;
        const listQuiz = state.loadedQuizzes.find(q => q.id === state.currentQuiz.id);
        if (listQuiz) listQuiz.duration = durationStr;
      } catch (err) {
        console.error('Error updating quiz duration:', err);
      }
    }

    async function updateQuestion(questionId, text, points) {
      try {
        const body = {};
        if (text !== undefined) body.text = text;
        if (points !== undefined) body.points = points === '' ? null : Number(points);
        await fetch(`${API.QUESTIONS}/${questionId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify(body)
        });
        // Update local state
        const q = state.currentQuiz?.questions?.find(x => x.id === questionId);
        if (q) {
          if (text !== undefined) q.text = text;
          if (points !== undefined) q.points = points === '' ? null : Number(points);
        }
      } catch (err) {
        console.error('Error updating question:', err);
      }
    }

    async function deleteQuestion(questionId) {
      if (!confirm('Delete this question?')) return;
      try {
        await fetch(`${API.QUESTIONS}/${questionId}`, { method: 'DELETE', credentials: 'include' });
        if (state.currentQuiz) {
          state.currentQuiz.questions = state.currentQuiz.questions.filter(q => q.id !== questionId);
          renderQuestions();
          renderWarnings();
        }
      } catch (err) {
        console.error('Error deleting question:', err);
      }
    }

    async function addQuestion() {
      if (!state.currentQuiz) return;
      const order = (state.currentQuiz.questions?.length ?? 0) + 1;
      try {
        const res = await fetch(API.QUESTIONS, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ quizId: state.currentQuiz.id, text: 'New question', order, points: 1 })
        });
        if (!res.ok) throw new Error('Failed to add');
        const newQuestionId = await res.json();
        state.currentQuiz.questions = state.currentQuiz.questions || [];
        state.currentQuiz.questions.push({ id: newQuestionId, text: 'New question', order, points: 1, options: [] });
        renderQuestions();
        renderWarnings();
      } catch (err) {
        console.error('Error adding question:', err);
      }
    }

    async function updateOption(optionId, text, isCorrect) {
      try {
        const body = {};
        if (text !== undefined) body.text = text;
        if (isCorrect !== undefined) body.isCorrect = isCorrect;
        await fetch(`${API.OPTIONS}/${optionId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify(body)
        });
        // Update local state
        for (const q of (state.currentQuiz?.questions || [])) {
          const opt = q.options?.find(o => o.id === optionId);
          if (opt) {
            if (text !== undefined) opt.text = text;
            if (isCorrect !== undefined) {
              opt.isCorrect = isCorrect;
              renderWarnings();
            }
            break;
          }
        }
      } catch (err) {
        console.error('Error updating option:', err);
      }
    }

    async function deleteOption(optionId) {
      try {
        await fetch(`${API.OPTIONS}/${optionId}`, { method: 'DELETE', credentials: 'include' });
        for (const q of (state.currentQuiz?.questions || [])) {
          const idx = q.options?.findIndex(o => o.id === optionId);
          if (idx !== undefined && idx >= 0) {
            q.options.splice(idx, 1);
            break;
          }
        }
        renderQuestions();
        renderWarnings();
      } catch (err) {
        console.error('Error deleting option:', err);
      }
    }

    async function addOption(questionId) {
      const question = state.currentQuiz?.questions?.find(q => q.id === questionId);
      const order = (question?.options?.length ?? 0) + 1;
      try {
        const res = await fetch(API.OPTIONS, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify({ questionId, text: 'New option', order, isCorrect: false })
        });
        if (!res.ok) throw new Error('Failed to add');
        const newOptionId = await res.json();
        if (question) {
          question.options = question.options || [];
          question.options.push({ id: newOptionId, text: 'New option', order, isCorrect: false });
        }
        renderQuestions();
        renderWarnings();
      } catch (err) {
        console.error('Error adding option:', err);
      }
    }

    // ========================================================================
    // Event Handlers
    // ========================================================================
    function handleSearchInput() {
      const value = el.searchInput?.value?.trim() || '';
      if (state.searchTimeout) clearTimeout(state.searchTimeout);
      state.searchTimeout = setTimeout(() => {
        state.searchQuery = value;
        fetchQuizzes(0);
      }, SEARCH_DEBOUNCE_MS);
    }

    function handleQuizListClick(e) {
      const target = e.target;
      const deleteBtn = target.closest('[data-delete-quiz]');
      if (deleteBtn) {
        e.stopPropagation();
        deleteQuiz(deleteBtn.dataset.deleteQuiz);
        return;
      }
      if (target.closest('#quizLoadMore')) {
        fetchQuizzes(state.currentPage + 1);
        return;
      }
      const quizItem = target.closest('.quiz-list-item');
      if (quizItem?.dataset.quizId) {
        openEditModal(quizItem.dataset.quizId);
      }
    }

    function handleEditBodyClick(e) {
      const target = e.target;

      const deleteQ = target.closest('[data-delete-question]');
      if (deleteQ) {
        deleteQuestion(deleteQ.dataset.deleteQuestion);
        return;
      }

      const deleteO = target.closest('[data-delete-option]');
      if (deleteO) {
        deleteOption(deleteO.dataset.deleteOption);
        return;
      }

      const toggleC = target.closest('[data-toggle-correct]');
      if (toggleC) {
        const optId = toggleC.dataset.toggleCorrect;
        const isCorrect = toggleC.classList.contains('is-correct');
        toggleC.classList.toggle('is-correct');
        updateOption(optId, undefined, !isCorrect);
        return;
      }

      const addO = target.closest('[data-add-option]');
      if (addO) {
        addOption(addO.dataset.addOption);
        return;
      }
    }

    function handleEditBodyInput(e) {
      const target = e.target;
      const questionEl = target.closest('.qe-question');

      if (questionEl && target.classList.contains('qe-question-input')) {
        const qId = questionEl.dataset.questionId;
        debounce(() => updateQuestion(qId, target.value, undefined), `q-text-${qId}`, SAVE_DEBOUNCE_MS);
        return;
      }

      if (questionEl && target.classList.contains('qe-question-points')) {
        const qId = questionEl.dataset.questionId;
        debounce(() => updateQuestion(qId, undefined, target.value), `q-pts-${qId}`, SAVE_DEBOUNCE_MS);
        return;
      }

      const optionEl = target.closest('.qe-option');
      if (optionEl && target.classList.contains('qe-option-input')) {
        const oId = optionEl.dataset.optionId;
        debounce(() => updateOption(oId, target.value, undefined), `o-text-${oId}`, SAVE_DEBOUNCE_MS);
        return;
      }
    }

    function handleEditNameInput() {
      const name = el.editName.value.trim();
      if (name) {
        debounce(() => updateQuizName(name), 'quiz-name', SAVE_DEBOUNCE_MS);
      }
    }

    function handleEditDurationInput() {
      const durationStr = parseDurationInput(el.editDuration.value);
      if (durationStr) {
        debounce(() => updateQuizDuration(durationStr), 'quiz-duration', SAVE_DEBOUNCE_MS);
      }
    }

    // ========================================================================
    // Event Listeners
    // ========================================================================
    function initEventListeners() {
      // Popover
      el.quizzesBtn?.addEventListener('click', togglePopover);
      el.popover.querySelectorAll('[data-quiz-close]').forEach(btn => btn.addEventListener('click', closePopover));
      el.searchInput?.addEventListener('input', handleSearchInput);
      el.quizList?.addEventListener('click', handleQuizListClick);

      // Create modal
      el.createBtn?.addEventListener('click', openCreateModal);
      el.createForm?.addEventListener('submit', e => { e.preventDefault(); createQuiz(); });
      el.createModal?.querySelectorAll('[data-close-create-modal]').forEach(btn => btn.addEventListener('click', closeCreateModal));

      // Edit modal
      el.editBody?.addEventListener('click', handleEditBodyClick);
      el.editBody?.addEventListener('input', handleEditBodyInput);
      el.editName?.addEventListener('input', handleEditNameInput);
      el.editDuration?.addEventListener('input', handleEditDurationInput);
      el.addQuestionBtn?.addEventListener('click', addQuestion);
      el.editCloseBtn?.addEventListener('click', closeEditModal);
      el.editDoneBtn?.addEventListener('click', closeEditModal);
      el.editModal?.querySelectorAll('[data-close-edit-modal]').forEach(btn => btn.addEventListener('click', closeEditModal));

      // Escape key
      document.addEventListener('keydown', e => {
        if (e.key === 'Escape') {
          if (el.editModal?.classList.contains('open')) {
            closeEditModal();
          } else if (el.createModal?.classList.contains('open')) {
            closeCreateModal();
          } else if (state.popoverOpen) {
            closePopover();
          }
        }
      });

      // Click outside popover
      document.addEventListener('click', e => {
        if (!state.popoverOpen) return;
        if (el.editModal?.classList.contains('open')) return;
        if (el.createModal?.classList.contains('open')) return;
        const target = e.target;
        if (!el.popover.contains(target) && !el.quizzesBtn?.contains(target)) {
          closePopover();
        }
      });
    }

    initEventListeners();
  }
})();
