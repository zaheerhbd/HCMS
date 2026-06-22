/* ═══════════════════════════════════════════════════════════════════
   HCMS PROTOTYPE — app.js
   SPA router, view renderers, modal/toast/tab utilities.
   All data comes from data.js (loaded before this script).
   ═══════════════════════════════════════════════════════════════════ */

/* ────────────────────────────────────────────────────────────────────
   SECTION 1 — BOOTSTRAP
   Runs once the DOM is ready. Wires up login and loads the shell.
   ──────────────────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  wireLogin();
  wireNotificationBell();
  wireGlobalSearch();
});

/* ────────────────────────────────────────────────────────────────────
   SECTION 2 — LOGIN
   Validates demo credentials, hides login screen, shows app shell.
   In the real app: POST /api/auth/login → JWT + refresh token.
   ──────────────────────────────────────────────────────────────────── */
function wireLogin() {
  const form = document.getElementById('login-form');
  if (!form) return;

  form.addEventListener('submit', e => {
    e.preventDefault();
    const username = document.getElementById('username').value.trim().toLowerCase();
    const password = document.getElementById('password').value;

    const match = DEMO_USERS[username];
    if (match && match.password === password) {
      // Success — swap screens
      document.getElementById('login-screen').style.display = 'none';
      document.getElementById('app-shell').classList.add('visible');

      // Populate topbar with logged-in user name
      const activeUser = match.user;
      document.getElementById('user-display-name').textContent = activeUser.name;
      document.getElementById('user-avatar-initials').textContent = activeUser.initials;

      // Navigate to dashboard as first screen
      navigate('dashboard');
      renderNotificationBadge();
    } else {
      showToast('Invalid username or password. Try: coordinator / demo', 'error');
    }
  });
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 3 — SPA ROUTER
   Shows one .view div at a time. Updates sidebar active state.
   Views: dashboard | patients | patient-detail | cases | case-detail |
          tasks | appointments | fhir | reports | admin-users | audit
   ──────────────────────────────────────────────────────────────────── */
let currentView   = null;  // currently shown view id
let selectedPatient = null; // patient object when drilling into detail
let selectedCase    = null; // case object when drilling into detail

function navigate(viewId, data) {
  // Hide all views
  document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));

  // Show the target view
  const target = document.getElementById('view-' + viewId);
  if (!target) { console.warn('View not found:', viewId); return; }
  target.classList.add('active');

  currentView = viewId;

  // Update sidebar active link
  document.querySelectorAll('.nav-link').forEach(l => {
    l.classList.toggle('active', l.dataset.view === viewId);
  });

  // Close notification panel if open
  document.getElementById('notif-panel')?.classList.remove('open');

  // Render dynamic content for each view
  switch (viewId) {
    case 'dashboard':      renderDashboard();     break;
    case 'patients':       renderPatients();      break;
    case 'patient-detail':
      selectedPatient = data || selectedPatient;
      renderPatientDetail();
      break;
    case 'cases':          renderCases();         break;
    case 'case-detail':
      selectedCase = data || selectedCase;
      renderCaseDetail();
      break;
    case 'tasks':          renderTasks();         break;
    case 'appointments':   renderAppointments();  break;
    case 'fhir':           renderFhir();          break;
    case 'reports':        renderReports();       break;
    case 'admin-users':    renderAdminUsers();    break;
    case 'audit':          renderAudit();         break;
  }
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 4 — DASHBOARD RENDERER
   Shows KPI cards, recent cases, upcoming appointments, overdue tasks.
   In the real app: GET /api/dashboard/summary
   ──────────────────────────────────────────────────────────────────── */
function renderDashboard() {
  // KPI counts
  const activeCases   = CASES.filter(c => !['Closed'].includes(c.status)).length;
  const criticalCases = CASES.filter(c => c.priority === 'Critical').length;
  const overdueTasks  = TASKS.filter(t => t.overdue && t.status !== 'Completed').length;
  const todayAppts    = APPOINTMENTS.length;

  setText('dash-active-cases',   activeCases);
  setText('dash-critical-cases', criticalCases);
  setText('dash-overdue-tasks',  overdueTasks);
  setText('dash-today-appts',    todayAppts);

  // Recent cases table — show 5 most recent
  const tbody = document.getElementById('dash-recent-cases');
  tbody.innerHTML = CASES.slice(0, 5).map(c => `
    <tr onclick="navigate('case-detail', CASES.find(x=>x.id==='${c.id}'))" title="Open case">
      <td><strong>${c.caseNumber}</strong></td>
      <td>${c.patientName}</td>
      <td>${c.title.length > 45 ? c.title.slice(0, 45) + '…' : c.title}</td>
      <td><span class="badge badge-${statusClass(c.status)}">${formatStatus(c.status)}</span></td>
      <td><span class="badge badge-${priorityClass(c.priority)}">${c.priority}</span></td>
      <td>${c.assignedTo}</td>
    </tr>
  `).join('');

  // Overdue tasks — show up to 3
  const taskList = document.getElementById('dash-overdue-task-list');
  const overdue  = TASKS.filter(t => t.overdue && t.status !== 'Completed');
  taskList.innerHTML = overdue.length === 0
    ? '<p style="color:var(--muted);font-size:13px;">No overdue tasks 🎉</p>'
    : overdue.map(t => `
        <div class="task-card priority-${t.priority.toLowerCase()}" style="margin-bottom:8px;">
          <div>
            <div class="task-title">${t.title}</div>
            <div class="task-meta">
              <span>${t.caseNumber}</span>
              <span class="task-overdue">Due ${t.dueDate}</span>
            </div>
          </div>
        </div>
      `).join('');

  // Upcoming appointments
  const apptList = document.getElementById('dash-appt-list');
  apptList.innerHTML = APPOINTMENTS.slice(0, 3).map(a => `
    <div class="appt-card">
      <div class="appt-time">
        <div>${a.time}</div>
        <div class="date">${formatDate(a.date)}</div>
      </div>
      <div class="appt-divider"></div>
      <div class="appt-info">
        <div class="appt-title">${a.patientName}</div>
        <div class="appt-sub">${a.title}</div>
        <div class="appt-sub">${a.provider} · ${a.location}</div>
      </div>
      <span class="badge badge-${a.status === 'Confirmed' ? 'green' : 'blue'}">${a.status}</span>
    </div>
  `).join('');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 5 — PATIENTS LIST RENDERER
   Search + risk filter. Click row → patient detail.
   In the real app: GET /api/patients?search=&riskLevel=&page=
   ──────────────────────────────────────────────────────────────────── */
function renderPatients(search = '', riskFilter = '') {
  const tbody = document.getElementById('patients-tbody');
  let data = PATIENTS;

  if (search)     data = data.filter(p => (p.firstName + ' ' + p.lastName + p.mrn).toLowerCase().includes(search.toLowerCase()));
  if (riskFilter) data = data.filter(p => p.riskLevel === riskFilter);

  tbody.innerHTML = data.length === 0
    ? '<tr><td colspan="7" class="empty-state">No patients match your search.</td></tr>'
    : data.map(p => `
        <tr onclick="navigate('patient-detail', PATIENTS.find(x=>x.id==='${p.id}'))" title="Open patient record">
          <td><strong>${p.mrn}</strong></td>
          <td>
            <div style="display:flex;align-items:center;gap:8px;">
              <div style="width:30px;height:30px;border-radius:50%;background:var(--primary-bg);color:var(--primary);
                          display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;">
                ${p.firstName[0]}${p.lastName[0]}
              </div>
              <span>${p.firstName} ${p.lastName}</span>
            </div>
          </td>
          <td>${p.dob} (${p.age} yrs)</td>
          <td><span class="badge badge-${riskClass(p.riskLevel)}">${p.riskLevel}</span></td>
          <td>${p.primaryDiagnosis}</td>
          <td>${p.caseCount} case${p.caseCount !== 1 ? 's' : ''}</td>
          <td>${p.assignedCoordinator}</td>
        </tr>
      `).join('');

  // Wire search + filter inputs (only wire once)
  const searchInput  = document.getElementById('patient-search');
  const riskSelect   = document.getElementById('patient-risk-filter');
  if (searchInput && !searchInput.dataset.wired) {
    searchInput.dataset.wired = '1';
    searchInput.addEventListener('input',  () => renderPatients(searchInput.value, riskSelect.value));
    riskSelect.addEventListener('change',  () => renderPatients(searchInput.value, riskSelect.value));
  }
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 6 — PATIENT DETAIL RENDERER
   Demographic hero card + tabbed content (Cases, Documents, FHIR).
   In the real app: GET /api/patients/{id}
   ──────────────────────────────────────────────────────────────────── */
function renderPatientDetail() {
  const p = selectedPatient;
  if (!p) return;

  // Breadcrumb
  setText('pd-breadcrumb', p.firstName + ' ' + p.lastName);

  // Hero card
  setText('pd-name',     p.firstName + ' ' + p.lastName);
  setText('pd-mrn',      p.mrn);
  setText('pd-subtitle', `${p.age} yrs · ${p.gender} · ${p.primaryDiagnosis}`);
  setText('pd-initials', p.firstName[0] + p.lastName[0]);

  // Demographics grid
  setText('pd-dob',       p.dob);
  setText('pd-gender',    p.gender);
  setText('pd-phone',     p.phone);
  setText('pd-email',     p.email || '—');
  setText('pd-address',   p.address);
  setText('pd-insurance', `${p.insurance} (${p.insuranceId})`);
  setText('pd-coord',     p.assignedCoordinator);
  setText('pd-last',      p.lastContact);

  // Risk badge
  const riskEl = document.getElementById('pd-risk');
  riskEl.textContent  = p.riskLevel;
  riskEl.className    = `badge badge-${riskClass(p.riskLevel)}`;

  // Cases for this patient
  const patientCases = CASES.filter(c => c.patientId === p.id);
  const casesTbody   = document.getElementById('pd-cases-tbody');
  casesTbody.innerHTML = patientCases.map(c => `
    <tr onclick="navigate('case-detail', CASES.find(x=>x.id==='${c.id}'))" title="Open case">
      <td><strong>${c.caseNumber}</strong></td>
      <td>${c.title}</td>
      <td><span class="badge badge-${statusClass(c.status)}">${formatStatus(c.status)}</span></td>
      <td><span class="badge badge-${priorityClass(c.priority)}">${c.priority}</span></td>
      <td>${c.openedDate}</td>
      <td>${c.assignedTo}</td>
    </tr>
  `).join('') || '<tr><td colspan="6" style="text-align:center;color:var(--muted);">No cases yet</td></tr>';

  // Activate first tab
  showTab('pd-tabs', 0);
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 7 — CASES LIST RENDERER
   Filter by status + priority. Click row → case detail.
   In the real app: GET /api/cases?status=&priority=&page=
   ──────────────────────────────────────────────────────────────────── */
function renderCases(statusFilter = '', priorityFilter = '') {
  const tbody = document.getElementById('cases-tbody');
  let data = CASES;

  if (statusFilter)   data = data.filter(c => c.status   === statusFilter);
  if (priorityFilter) data = data.filter(c => c.priority === priorityFilter);

  tbody.innerHTML = data.map(c => `
    <tr onclick="navigate('case-detail', CASES.find(x=>x.id==='${c.id}'))" title="Open case">
      <td><strong>${c.caseNumber}</strong></td>
      <td>${c.patientName}</td>
      <td>${c.title.length > 50 ? c.title.slice(0, 50) + '…' : c.title}</td>
      <td><span class="badge badge-${statusClass(c.status)}">${formatStatus(c.status)}</span></td>
      <td><span class="badge badge-${priorityClass(c.priority)}">${c.priority}</span></td>
      <td>${c.category}</td>
      <td>${c.targetCloseDate}</td>
      <td>${c.assignedTo}</td>
    </tr>
  `).join('');

  // Wire filters
  const statusSel   = document.getElementById('case-status-filter');
  const prioritySel = document.getElementById('case-priority-filter');
  if (statusSel && !statusSel.dataset.wired) {
    statusSel.dataset.wired = '1';
    statusSel.addEventListener('change',   () => renderCases(statusSel.value, prioritySel.value));
    prioritySel.addEventListener('change', () => renderCases(statusSel.value, prioritySel.value));
  }
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 8 — CASE DETAIL RENDERER
   5 tabs: Overview | Notes | Tasks | Documents | Team & History
   In the real app: GET /api/cases/{id}  + /api/cases/{id}/notes  etc.
   ──────────────────────────────────────────────────────────────────── */
function renderCaseDetail() {
  const c = selectedCase;
  if (!c) return;

  // Header info
  setText('cd-case-number', c.caseNumber);
  setText('cd-patient-link', c.patientName);
  setText('cd-title',  c.title);
  setText('cd-summary', c.summary);
  setText('cd-opened',  c.openedDate);
  setText('cd-target',  c.targetCloseDate);
  setText('cd-assigned', c.assignedTo);
  setText('cd-category', c.category);

  // Status + Priority badges
  const statusEl = document.getElementById('cd-status');
  statusEl.textContent = formatStatus(c.status);
  statusEl.className   = `badge badge-${statusClass(c.status)}`;

  const priEl = document.getElementById('cd-priority');
  priEl.textContent = c.priority;
  priEl.className   = `badge badge-${priorityClass(c.priority)}`;

  // Notes tab
  const notesList = document.getElementById('cd-notes-list');
  notesList.innerHTML = c.notes.length === 0
    ? '<p style="color:var(--muted);">No notes yet. Add the first one.</p>'
    : c.notes.map(n => `
        <div style="border-left:3px solid var(--primary-bg);padding:12px 16px;margin-bottom:12px;background:var(--bg);border-radius:0 6px 6px 0;">
          <div style="font-size:12px;color:var(--muted);margin-bottom:6px;">
            <strong>${n.author}</strong> · ${n.date}
          </div>
          <div style="font-size:13.5px;">${n.text}</div>
        </div>
      `).join('');

  // Tasks tab
  const caseTasks  = TASKS.filter(t => c.tasks.includes(t.id));
  const tasksList  = document.getElementById('cd-tasks-list');
  tasksList.innerHTML = caseTasks.length === 0
    ? '<p style="color:var(--muted);">No tasks assigned to this case.</p>'
    : caseTasks.map(t => `
        <div class="task-card priority-${t.priority.toLowerCase()}">
          <div>
            <div class="task-title">${t.title}</div>
            <div class="task-meta">
              <span>Due: ${t.dueDate}</span>
              <span class="badge badge-${priorityClass(t.priority)} badge" style="font-size:10px;">${t.priority}</span>
              ${t.overdue ? '<span class="task-overdue">OVERDUE</span>' : ''}
            </div>
          </div>
        </div>
      `).join('');

  // Team tab
  const teamList = document.getElementById('cd-team-list');
  teamList.innerHTML = c.team.map(member => `
    <div style="display:flex;align-items:center;gap:10px;padding:10px 0;border-bottom:1px solid var(--border);">
      <div style="width:36px;height:36px;border-radius:50%;background:var(--primary-bg);color:var(--primary);
                  display:flex;align-items:center;justify-content:center;font-weight:700;font-size:13px;">
        ${member.split(' ').map(w => w[0]).filter((_,i,a) => i < 2).join('')}
      </div>
      <span style="font-size:13.5px;">${member}</span>
    </div>
  `).join('');

  // Activate first tab
  showTab('cd-tabs', 0);

  // Wire patient link
  const patLink = document.getElementById('cd-patient-link');
  if (patLink) {
    patLink.style.cursor = 'pointer';
    patLink.style.color  = 'var(--primary)';
    patLink.onclick = () => navigate('patient-detail', PATIENTS.find(p => p.id === c.patientId));
  }
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 9 — MY TASKS RENDERER
   Filter by status. Checkbox toggles completion.
   In the real app: GET /api/tasks?assignedToMe=true
   ──────────────────────────────────────────────────────────────────── */
function renderTasks(filter = 'all') {
  const container = document.getElementById('tasks-list');
  let data = TASKS.filter(t => t.assignedTo === CURRENT_USER.name);

  if (filter === 'pending')   data = data.filter(t => t.status === 'Pending');
  if (filter === 'overdue')   data = data.filter(t => t.overdue);
  if (filter === 'completed') data = data.filter(t => t.status === 'Completed');

  container.innerHTML = data.length === 0
    ? `<div class="empty-state"><div class="empty-icon">✅</div><h4>No tasks here</h4></div>`
    : data.map(t => `
        <div class="task-card priority-${t.priority.toLowerCase()}" id="tc-${t.id}">
          <div class="task-check ${t.status === 'Completed' ? 'checked' : ''}"
               onclick="toggleTask('${t.id}', this)" title="Mark complete"></div>
          <div style="flex:1;">
            <div class="task-title" style="${t.status === 'Completed' ? 'text-decoration:line-through;color:var(--muted)' : ''}">${t.title}</div>
            <div class="task-meta">
              <span>📋 ${t.caseNumber}</span>
              <span>👤 ${t.patientName}</span>
              <span>📅 Due: ${t.dueDate}</span>
              ${t.overdue && t.status !== 'Completed' ? '<span class="task-overdue">⚠ OVERDUE</span>' : ''}
            </div>
          </div>
          <span class="badge badge-${priorityClass(t.priority)}">${t.priority}</span>
        </div>
      `).join('');

  // Wire filter buttons
  document.querySelectorAll('[data-task-filter]').forEach(btn => {
    btn.onclick = () => {
      document.querySelectorAll('[data-task-filter]').forEach(b => b.classList.remove('btn-primary'));
      btn.classList.add('btn-primary');
      renderTasks(btn.dataset.taskFilter);
    };
  });
}

// Toggle a task's completed state (prototype only — in real app: PUT /api/tasks/{id})
function toggleTask(id, el) {
  const task = TASKS.find(t => t.id === id);
  if (!task) return;
  task.status = task.status === 'Completed' ? 'Pending' : 'Completed';
  el.classList.toggle('checked', task.status === 'Completed');
  showToast(task.status === 'Completed' ? 'Task marked complete ✓' : 'Task reopened', 'success');
  renderTasks(); // re-render
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 10 — APPOINTMENTS RENDERER
   Grouped by date; click row shows details (prototype toast).
   In the real app: GET /api/appointments?from=today&days=14
   ──────────────────────────────────────────────────────────────────── */
function renderAppointments() {
  const container = document.getElementById('appt-list');

  // Group by date
  const grouped = {};
  APPOINTMENTS.forEach(a => {
    if (!grouped[a.date]) grouped[a.date] = [];
    grouped[a.date].push(a);
  });

  container.innerHTML = Object.entries(grouped).sort().map(([date, appts]) => `
    <div style="margin-bottom:24px;">
      <h4 style="font-size:13px;color:var(--muted);text-transform:uppercase;letter-spacing:.6px;
                 margin-bottom:12px;padding-bottom:6px;border-bottom:1px solid var(--border);">
        ${formatDate(date)}
      </h4>
      ${appts.map(a => `
        <div class="appt-card" style="margin-bottom:10px;cursor:pointer;"
             onclick="showToast('${a.title} — ${a.provider} · ${a.location}', 'info')">
          <div class="appt-time">
            <div>${a.time}</div>
            <div class="date">${a.duration}</div>
          </div>
          <div class="appt-divider"></div>
          <div class="appt-info">
            <div class="appt-title">${a.patientName}</div>
            <div class="appt-sub">${a.title}</div>
            <div class="appt-sub" style="margin-top:2px;">👤 ${a.provider} · 📍 ${a.location}</div>
          </div>
          <div style="display:flex;flex-direction:column;align-items:flex-end;gap:6px;">
            <span class="badge badge-${a.type === 'Telehealth' ? 'teal' : 'blue'}">${a.type}</span>
            <span class="badge badge-${a.status === 'Confirmed' ? 'green' : a.status === 'Scheduled' ? 'blue' : 'orange'}">${a.status}</span>
          </div>
        </div>
      `).join('')}
    </div>
  `).join('');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 11 — FHIR EXPLORER RENDERER
   Lets user pick a patient and see their FHIR R4 Bundle.
   In the real app: GET /fhir/R4/Patient/{id}/$everything
   The JSON is syntax-highlighted via formatFhirJson().
   ──────────────────────────────────────────────────────────────────── */
function renderFhir(patientId = 'pat-001') {
  // Populate patient selector
  const sel = document.getElementById('fhir-patient-select');
  if (sel && !sel.dataset.wired) {
    sel.innerHTML = PATIENTS.map(p =>
      `<option value="${p.id}">${p.firstName} ${p.lastName} (${p.mrn})</option>`
    ).join('');
    sel.dataset.wired = '1';
    sel.addEventListener('change', () => renderFhir(sel.value));
  }

  // Show selected patient info
  const patient = PATIENTS.find(p => p.id === patientId) || PATIENTS[0];
  setText('fhir-patient-name', `${patient.firstName} ${patient.lastName} — ${patient.mrn}`);
  setText('fhir-endpoint', `GET /fhir/R4/Patient/${patientId}/$everything`);

  // Render syntax-highlighted FHIR JSON
  const panel = document.getElementById('fhir-json-panel');
  panel.innerHTML = formatFhirJson(JSON.stringify(FHIR_BUNDLE, null, 2));

  // Wire copy button
  const copyBtn = document.getElementById('fhir-copy-btn');
  if (copyBtn && !copyBtn.dataset.wired) {
    copyBtn.dataset.wired = '1';
    copyBtn.onclick = () => {
      navigator.clipboard?.writeText(JSON.stringify(FHIR_BUNDLE, null, 2))
        .then(() => showToast('FHIR Bundle copied to clipboard', 'success'));
    };
  }
}

/* Syntax highlights a JSON string for the dark FHIR panel */
function formatFhirJson(json) {
  return json
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    // Keys
    .replace(/"(\w+)"(?=\s*:)/g, '<span class="fhir-key">"$1"</span>')
    // String values (after colon)
    .replace(/: "([^"]*)"(,?)/g, ': <span class="fhir-string">"$1"</span>$2')
    // Numbers
    .replace(/: (\d+)(,?)/g, ': <span class="fhir-num">$1</span>$2')
    // Booleans
    .replace(/: (true|false)(,?)/g, ': <span class="fhir-bool">$1</span>$2')
    // Null
    .replace(/: (null)(,?)/g, ': <span class="fhir-null">$1</span>$2');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 11b — FHIR PATIENT IMPORT
   Handles the Import Patient modal on the FHIR Explorer screen.

   Flow:
     1. User pastes a FHIR R4 Patient resource JSON into the textarea
        (or clicks "Use Sample" to pre-fill it)
     2. "Parse & Preview" button validates the JSON and shows a
        human-readable preview of the fields that will be mapped
     3. "Confirm Import" adds the patient to the PATIENTS array,
        logs it in FHIR_IMPORT_LOGS + AUDIT_LOG, and navigates
        to the new patient's detail screen

   In the real app this is: POST /fhir/R4/Patient
   The .NET controller validates with Firely SDK, maps to the Patient
   entity via IFhirMappingService, saves via EF Core, and returns
   a 201 Created with the new internal MRN.
   ──────────────────────────────────────────────────────────────────── */

// Fill the textarea with the sample FHIR Patient from data.js
function fillSampleFhirPatient() {
  const ta = document.getElementById('fhir-import-input');
  ta.value = JSON.stringify(SAMPLE_FHIR_PATIENT, null, 2);
  // Clear any previous preview
  document.getElementById('fhir-import-preview').style.display = 'none';
  document.getElementById('fhir-import-error').style.display   = 'none';
  document.getElementById('fhir-confirm-btn').disabled = true;
}

// Parse the pasted FHIR Patient JSON and show a field-mapping preview
function parseFhirImport() {
  const errorEl   = document.getElementById('fhir-import-error');
  const previewEl = document.getElementById('fhir-import-preview');
  const confirmBtn = document.getElementById('fhir-confirm-btn');

  errorEl.style.display   = 'none';
  previewEl.style.display = 'none';
  confirmBtn.disabled = true;

  const raw = document.getElementById('fhir-import-input').value.trim();
  if (!raw) {
    showError('Paste a FHIR R4 Patient JSON first.'); return;
  }

  // Step 1: valid JSON?
  let fhir;
  try { fhir = JSON.parse(raw); }
  catch { showError('Invalid JSON — check for missing commas or brackets.'); return; }

  // Step 2: must be a Patient resource
  if (fhir.resourceType !== 'Patient') {
    showError(`Expected resourceType "Patient" but got "${fhir.resourceType}". Only Patient resources can be imported here.`);
    return;
  }

  // Step 3: must have at least a name
  if (!fhir.name || !fhir.name[0]) {
    showError('FHIR Patient must include at least one name entry.');
    return;
  }

  // Map FHIR fields → internal patient shape
  const mapped = mapFhirToPatient(fhir);

  // Show the preview table so user can confirm before committing
  previewEl.innerHTML = `
    <div style="margin-bottom:10px;font-size:13px;font-weight:600;color:var(--accent);">
      ✓ Valid FHIR Patient — review the mapped fields below before importing:
    </div>
    <table style="font-size:13px;width:100%;">
      <thead>
        <tr>
          <th style="text-align:left;padding:6px 10px;background:var(--bg);color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.5px;">FHIR Field</th>
          <th style="text-align:left;padding:6px 10px;background:var(--bg);color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.5px;">Maps To</th>
          <th style="text-align:left;padding:6px 10px;background:var(--bg);color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.5px;">Value</th>
        </tr>
      </thead>
      <tbody>
        ${previewRow('name[0].family + given', 'firstName + lastName', mapped.firstName + ' ' + mapped.lastName)}
        ${previewRow('gender', 'gender', mapped.gender)}
        ${previewRow('birthDate', 'dob', mapped.dob)}
        ${previewRow('telecom (phone)', 'phone', mapped.phone || '—')}
        ${previewRow('telecom (email)', 'email', mapped.email || '—')}
        ${previewRow('address[0].text', 'address', mapped.address || '—')}
        ${previewRow('identifier (external)', 'External MRN', mapped.externalId || '—')}
        ${previewRow('(auto-generated)', 'Internal MRN', mapped.mrn)}
      </tbody>
    </table>
    <div style="margin-top:12px;font-size:12px;color:var(--muted);">
      ⚙ Source system: <strong>${mapped.sourceSystem}</strong> ·
      Import will be logged in FhirImportLogs with timestamp and user.
    </div>
  `;

  previewEl.style.display = 'block';
  confirmBtn.disabled = false;

  // Store mapped data on the button so confirmImport() can use it
  confirmBtn.dataset.mapped = JSON.stringify(mapped);

  function showError(msg) {
    errorEl.textContent = '✕ ' + msg;
    errorEl.style.display = 'block';
  }
}

// Small helper: one row in the preview table
function previewRow(fhirField, internalField, value) {
  return `
    <tr style="border-bottom:1px solid var(--border);">
      <td style="padding:7px 10px;font-family:monospace;font-size:12px;color:var(--accent);">${fhirField}</td>
      <td style="padding:7px 10px;color:var(--muted);">${internalField}</td>
      <td style="padding:7px 10px;font-weight:500;">${value}</td>
    </tr>`;
}

// Map a parsed FHIR Patient object → internal patient shape
function mapFhirToPatient(fhir) {
  const nameObj    = fhir.name?.[0] || {};
  const firstName  = (nameObj.given?.[0]) || '';
  const lastName   = nameObj.family || '';
  const dob        = fhir.birthDate || '';
  const gender     = fhir.gender
    ? fhir.gender.charAt(0).toUpperCase() + fhir.gender.slice(1)
    : 'Unknown';

  const phone = fhir.telecom?.find(t => t.system === 'phone')?.value || '';
  const email = fhir.telecom?.find(t => t.system === 'email')?.value || '';
  const address = fhir.address?.[0]?.text || '';

  // External identifier (e.g. Epic MRN)
  const extId      = fhir.identifier?.[0]?.value || '';
  const extSystem  = fhir.identifier?.[0]?.system || 'Unknown system';
  const sourceLabel = extSystem.includes('epic') ? 'Epic EHR'
                    : extSystem.includes('cerner') ? 'Cerner'
                    : extSystem;

  // Generate next internal MRN
  const nextNum  = String(PATIENTS.length + 1).padStart(5, '0');
  const newMrn   = `MRN-2026-00${nextNum}`;

  // Calculate approximate age
  const age = dob
    ? Math.floor((new Date() - new Date(dob)) / (365.25 * 24 * 60 * 60 * 1000))
    : null;

  return {
    id:          'pat-imp-' + Date.now(),
    mrn:         newMrn,
    firstName,
    lastName,
    dob,
    age,
    gender,
    phone,
    email,
    address,
    externalId:  extId,
    sourceSystem: sourceLabel,
    insurance:   'Unknown — to be verified',
    insuranceId: '',
    primaryDiagnosis: 'Pending assessment',
    riskLevel:   'Medium',
    caseCount:   0,
    lastContact: new Date().toISOString().slice(0, 10),
    assignedCoordinator: CURRENT_USER.name,
    status:      'Active',
  };
}

// Commit the import — called when user clicks "Confirm Import"
function confirmFhirImport() {
  const btn = document.getElementById('fhir-confirm-btn');
  const mapped = JSON.parse(btn.dataset.mapped || '{}');
  if (!mapped.id) return;

  // Add to in-memory patients list
  PATIENTS.push(mapped);

  // Log to FhirImportLogs (in real app: INSERT INTO FhirImportLogs)
  FHIR_IMPORT_LOGS.push({
    id:           'fimp-' + Date.now(),
    importedAt:   new Date().toLocaleString(),
    importedBy:   CURRENT_USER.name,
    sourceSystem: mapped.sourceSystem,
    externalId:   mapped.externalId,
    patientMrn:   mapped.mrn,
    patientName:  mapped.firstName + ' ' + mapped.lastName,
    status:       'Success',
    resourceType: 'Patient',
  });

  // Log to Audit Log (in real app: INSERT INTO AuditLogs)
  AUDIT_LOG.unshift({
    id:       'a-imp-' + Date.now(),
    ts:       new Date().toLocaleString(),
    user:     CURRENT_USER.name,
    action:   'CREATE',
    resource: 'Patient (FHIR Import)',
    detail:   `Imported patient ${mapped.firstName} ${mapped.lastName} from ${mapped.sourceSystem} → assigned ${mapped.mrn}`,
    category: 'write',
  });

  closeModal('modal-fhir-import');
  showToast(`Patient imported: ${mapped.firstName} ${mapped.lastName} → ${mapped.mrn}`, 'success');

  // Navigate straight to the new patient's detail screen
  setTimeout(() => navigate('patient-detail', mapped), 400);
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 12 — REPORTS RENDERER
   CSS bar chart (cases by month) + SVG donut (case status breakdown).
   In the real app: GET /api/reports/case-volume  +  /case-status
   ──────────────────────────────────────────────────────────────────── */
function renderReports() {
  // ── Bar chart: cases opened per month ──────────────────────────
  const monthData = [
    { label: 'Jan', value: 8  },
    { label: 'Feb', value: 12 },
    { label: 'Mar', value: 9  },
    { label: 'Apr', value: 15 },
    { label: 'May', value: 18 },
    { label: 'Jun', value: 11 },
  ];
  const maxVal = Math.max(...monthData.map(d => d.value));
  const barChart = document.getElementById('bar-chart');
  barChart.innerHTML = monthData.map(d => `
    <div style="flex:1;display:flex;flex-direction:column;align-items:center;">
      <div style="font-size:11px;color:var(--muted);margin-bottom:4px;">${d.value}</div>
      <div class="bar" style="height:${(d.value/maxVal)*120}px;background:var(--primary);width:100%;"></div>
      <div class="bar-label">${d.label}</div>
    </div>
  `).join('');

  // ── Donut chart: case status breakdown ─────────────────────────
  const statusData = [
    { label: 'In Progress',  count: 2, color: '#00897B' },
    { label: 'Open',         count: 2, color: '#1565C0' },
    { label: 'Escalated',    count: 1, color: '#C62828' },
    { label: 'Pending Info', count: 1, color: '#E65100' },
  ];
  const total = statusData.reduce((s, d) => s + d.count, 0);

  // Build SVG donut arcs
  let offset = 0;
  const r = 45, cx = 60, cy = 60, circumference = 2 * Math.PI * r;
  const arcs = statusData.map(d => {
    const fraction = d.count / total;
    const dash     = fraction * circumference;
    const arc = `<circle cx="${cx}" cy="${cy}" r="${r}"
                   fill="none" stroke="${d.color}" stroke-width="20"
                   stroke-dasharray="${dash} ${circumference - dash}"
                   stroke-dashoffset="${-offset * circumference}"
                   transform="rotate(-90 ${cx} ${cy})"/>`;
    offset += fraction;
    return arc;
  }).join('');

  const donutEl = document.getElementById('donut-svg');
  donutEl.innerHTML = arcs +
    `<text x="${cx}" y="${cy - 6}" text-anchor="middle" font-size="14" font-weight="700" fill="#1E293B">${total}</text>
     <text x="${cx}" y="${cy + 12}" text-anchor="middle" font-size="9" fill="#64748B">TOTAL</text>`;

  // Legend
  const legendEl = document.getElementById('donut-legend');
  legendEl.innerHTML = statusData.map(d => `
    <div class="legend-row">
      <div class="legend-dot" style="background:${d.color}"></div>
      <span>${d.label} — <strong>${d.count}</strong></span>
    </div>
  `).join('');

  // Summary stats
  setText('rpt-total-cases',  CASES.length);
  setText('rpt-total-patients', PATIENTS.length);
  setText('rpt-avg-days', '28');   // prototype hardcoded
  setText('rpt-closed-rate', '83%');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 13 — ADMIN USERS RENDERER
   Table of all users; role badge; deactivate action (prototype alert).
   In the real app: GET /api/admin/users  (Admin role required)
   ──────────────────────────────────────────────────────────────────── */
function renderAdminUsers() {
  const tbody = document.getElementById('users-tbody');
  tbody.innerHTML = USERS.map(u => `
    <tr>
      <td>
        <div style="display:flex;align-items:center;gap:8px;">
          <div style="width:32px;height:32px;border-radius:50%;background:var(--primary-bg);color:var(--primary);
                      display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;">
            ${u.name.split(' ').map(n => n[0]).join('')}
          </div>
          <div>
            <div style="font-weight:600;">${u.name}</div>
            <div style="font-size:11px;color:var(--muted);">${u.email}</div>
          </div>
        </div>
      </td>
      <td><span class="badge badge-${roleClass(u.role)}">${u.role}</span></td>
      <td>${u.department}</td>
      <td><span class="badge badge-${u.status === 'Active' ? 'green' : 'gray'}">${u.status}</span></td>
      <td style="font-size:12px;color:var(--muted);">${u.lastLogin}</td>
      <td>
        <button class="btn btn-sm btn-outline" onclick="showToast('User ${u.name} — edit form would open here (requires Admin role)', 'info')">Edit</button>
        ${u.status === 'Active'
          ? `<button class="btn btn-sm btn-danger" style="margin-left:4px;"
                     onclick="showToast('Deactivating ${u.name}… (confirmed via modal in real app)', 'error')">Deactivate</button>`
          : `<button class="btn btn-sm btn-accent"  style="margin-left:4px;"
                     onclick="showToast('Activating ${u.name}…', 'success')">Activate</button>`}
      </td>
    </tr>
  `).join('');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 14 — AUDIT LOG RENDERER
   Colour-coded by action type (read/write/delete/auth).
   In the real app: GET /api/audit?from=&to=&user=&action=
   ──────────────────────────────────────────────────────────────────── */
function renderAudit() {
  const tbody = document.getElementById('audit-tbody');
  tbody.innerHTML = AUDIT_LOG.map(a => `
    <tr class="audit-row-${a.category}">
      <td style="font-size:12px;font-family:monospace;">${a.ts}</td>
      <td>${a.user}</td>
      <td><span class="badge badge-${auditActionClass(a.action)}">${a.action}</span></td>
      <td>${a.resource}</td>
      <td style="font-size:12.5px;color:var(--muted);">${a.detail}</td>
    </tr>
  `).join('');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 15 — TAB SYSTEM
   showTab(groupId, index) activates panel at index within a tab group.
   ──────────────────────────────────────────────────────────────────── */
function showTab(groupId, index) {
  const group   = document.getElementById(groupId);
  if (!group) return;
  const buttons = group.querySelectorAll('.tab-btn');
  const panels  = group.querySelectorAll('.tab-panel');

  buttons.forEach((b, i) => b.classList.toggle('active', i === index));
  panels.forEach((p, i)  => p.classList.toggle('active', i === index));
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 16 — MODAL SYSTEM
   openModal(id) / closeModal(id) — wire close on overlay click too.
   ──────────────────────────────────────────────────────────────────── */
function openModal(id) {
  const overlay = document.getElementById(id);
  if (!overlay) return;
  overlay.classList.add('open');

  // Close when clicking the backdrop
  overlay.onclick = e => { if (e.target === overlay) closeModal(id); };
}

function closeModal(id) {
  document.getElementById(id)?.classList.remove('open');
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 17 — TOAST NOTIFICATIONS
   showToast(message, type) — type: 'success' | 'error' | 'info'
   Auto-dismisses after 3.5 seconds.
   ──────────────────────────────────────────────────────────────────── */
function showToast(message, type = 'info') {
  const container = document.getElementById('toast-container');
  const toast     = document.createElement('div');
  toast.className = `toast ${type}`;

  const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';
  toast.innerHTML = `<span>${icon}</span><span>${message}</span>`;

  container.appendChild(toast);

  // Fade out and remove after 3.5s
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transition = 'opacity .3s';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 18 — NOTIFICATION PANEL
   Toggles a slide-down panel listing recent notifications.
   ──────────────────────────────────────────────────────────────────── */
function wireNotificationBell() {
  const btn   = document.getElementById('notif-btn');
  const panel = document.getElementById('notif-panel');
  if (!btn || !panel) return;

  btn.onclick = e => {
    e.stopPropagation();
    panel.classList.toggle('open');
    if (panel.classList.contains('open')) renderNotifPanel();
  };

  // Close on outside click
  document.addEventListener('click', () => panel.classList.remove('open'));
}

function renderNotifPanel() {
  const list = document.getElementById('notif-list');
  list.innerHTML = NOTIFICATIONS.map(n => `
    <div class="notif-item ${n.unread ? 'unread' : ''}"
         onclick="showToast('${n.text}', 'info')">
      ${n.unread ? '<div class="notif-dot"></div>' : '<div style="width:8px"></div>'}
      <div>
        <div style="font-size:13px;">${n.text}</div>
        <div style="font-size:11px;color:var(--muted);margin-top:3px;">${n.time}</div>
      </div>
    </div>
  `).join('');
}

function renderNotificationBadge() {
  const unread = NOTIFICATIONS.filter(n => n.unread).length;
  const badge  = document.getElementById('notif-badge');
  if (badge) {
    badge.textContent = unread;
    badge.style.display = unread > 0 ? 'flex' : 'none';
  }
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 19 — GLOBAL SEARCH (prototype)
   Searches patients and cases; shows toast with count.
   In the real app: GET /api/search?q=
   ──────────────────────────────────────────────────────────────────── */
function wireGlobalSearch() {
  const input = document.getElementById('global-search');
  if (!input) return;

  input.addEventListener('keydown', e => {
    if (e.key !== 'Enter') return;
    const q = input.value.trim().toLowerCase();
    if (!q) return;

    const matchPat  = PATIENTS.filter(p => (p.firstName + ' ' + p.lastName + p.mrn).toLowerCase().includes(q));
    const matchCase = CASES.filter(c => (c.caseNumber + c.patientName + c.title).toLowerCase().includes(q));

    if (matchPat.length === 1 && matchCase.length === 0) {
      navigate('patient-detail', matchPat[0]);
    } else if (matchCase.length === 1 && matchPat.length === 0) {
      navigate('case-detail', matchCase[0]);
    } else {
      showToast(`Found ${matchPat.length} patient(s), ${matchCase.length} case(s) matching "${input.value}"`, 'info');
    }
    input.value = '';
  });
}

/* ────────────────────────────────────────────────────────────────────
   SECTION 20 — HELPER UTILITIES
   ──────────────────────────────────────────────────────────────────── */

// Set text content of an element by id
function setText(id, value) {
  const el = document.getElementById(id);
  if (el) el.textContent = value;
}

// Format a YYYY-MM-DD date string as "Jun 30, 2026"
function formatDate(dateStr) {
  const d = new Date(dateStr + 'T00:00:00');
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

// CSS class suffix for case status
function statusClass(status) {
  const map = { Draft: 'draft', Open: 'open', InProgress: 'progress', PendingInfo: 'pending', Closed: 'closed', Escalated: 'escalated' };
  return map[status] || 'gray';
}

// Human-readable status label
function formatStatus(status) {
  const map = { Draft: 'Draft', Open: 'Open', InProgress: 'In Progress', PendingInfo: 'Pending Info', Closed: 'Closed', Escalated: 'Escalated' };
  return map[status] || status;
}

// CSS class suffix for priority
function priorityClass(priority) {
  const map = { Critical: 'critical', High: 'high', Medium: 'medium', Low: 'low' };
  return map[priority] || 'gray';
}

// CSS class suffix for risk level
function riskClass(level) {
  const map = { Critical: 'critical', High: 'high', Medium: 'medium', Low: 'low' };
  return map[level] || 'gray';
}

// CSS class suffix for user role badge
function roleClass(role) {
  const map = { Admin: 'critical', Supervisor: 'orange', CareCoordinator: 'blue', Clinician: 'teal', ReadOnly: 'gray' };
  return map[role] || 'gray';
}

// CSS class for audit action
function auditActionClass(action) {
  const map = { VIEW: 'gray', UPDATE: 'blue', CREATE: 'green', DELETE: 'red', LOGIN: 'teal', EXPORT: 'orange' };
  return map[action] || 'gray';
}
