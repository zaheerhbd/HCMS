/* ═══════════════════════════════════════════════════════════════════
   HCMS PROTOTYPE — data.js
   All mock data used across the prototype screens.
   In the real app this comes from the .NET 8 REST API.
   ═══════════════════════════════════════════════════════════════════ */

/* ── CURRENT USER (logged-in care coordinator) ─────────────────────
   Roles: Admin | Supervisor | CareCoordinator | Clinician | ReadOnly
   ─────────────────────────────────────────────────────────────────── */
const CURRENT_USER = {
  id: 'u-001',
  name: 'Maria Santos',
  initials: 'MS',
  email: 'maria.santos@hcms.org',
  role: 'CareCoordinator',
  department: 'Case Management – Unit A',
};

/* ── PATIENTS ──────────────────────────────────────────────────────
   MRN format: MRN-YYYY-NNNNN
   ─────────────────────────────────────────────────────────────────── */
const PATIENTS = [
  {
    id: 'pat-001',
    mrn: 'MRN-2026-00001',
    firstName: 'Sarah',
    lastName: 'Johnson',
    dob: '1968-03-14',
    age: 58,
    gender: 'Female',
    phone: '(555) 201-4432',
    email: 'sarah.johnson@email.com',
    address: '142 Maple Street, Springfield, IL 62701',
    insurance: 'BlueCross BlueShield',
    insuranceId: 'BCB-882-001432',
    primaryDiagnosis: 'Type 2 Diabetes, Hypertension',
    riskLevel: 'High',
    caseCount: 3,
    lastContact: '2026-06-18',
    assignedCoordinator: 'Maria Santos',
    status: 'Active',
  },
  {
    id: 'pat-002',
    mrn: 'MRN-2026-00002',
    firstName: 'Robert',
    lastName: 'Chen',
    dob: '1951-07-22',
    age: 74,
    gender: 'Male',
    phone: '(555) 387-0912',
    email: 'r.chen@email.com',
    address: '88 Oak Avenue, Chicago, IL 60601',
    insurance: 'Medicare Advantage',
    insuranceId: 'MCR-774-009201',
    primaryDiagnosis: 'COPD, CHF',
    riskLevel: 'Critical',
    caseCount: 2,
    lastContact: '2026-06-20',
    assignedCoordinator: 'Maria Santos',
    status: 'Active',
  },
  {
    id: 'pat-003',
    mrn: 'MRN-2026-00003',
    firstName: 'Linda',
    lastName: 'Patel',
    dob: '1979-11-05',
    age: 46,
    gender: 'Female',
    phone: '(555) 492-7731',
    email: 'linda.patel@email.com',
    address: '310 Birch Lane, Naperville, IL 60540',
    insurance: 'Aetna',
    insuranceId: 'AET-331-449021',
    primaryDiagnosis: 'Lupus, Chronic Pain',
    riskLevel: 'Medium',
    caseCount: 1,
    lastContact: '2026-06-15',
    assignedCoordinator: 'James Rodriguez',
    status: 'Active',
  },
  {
    id: 'pat-004',
    mrn: 'MRN-2026-00004',
    firstName: 'Marcus',
    lastName: 'Williams',
    dob: '1943-09-30',
    age: 82,
    gender: 'Male',
    phone: '(555) 601-2288',
    email: '',
    address: '77 Pine Road, Aurora, IL 60505',
    insurance: 'Medicaid',
    insuranceId: 'MCD-009-882774',
    primaryDiagnosis: 'Alzheimer\'s Disease, Hypertension',
    riskLevel: 'Critical',
    caseCount: 2,
    lastContact: '2026-06-19',
    assignedCoordinator: 'Maria Santos',
    status: 'Active',
  },
  {
    id: 'pat-005',
    mrn: 'MRN-2026-00005',
    firstName: 'Emily',
    lastName: 'Torres',
    dob: '1990-02-18',
    age: 36,
    gender: 'Female',
    phone: '(555) 744-3310',
    email: 'emily.torres@email.com',
    address: '55 Cedar Court, Rockford, IL 61101',
    insurance: 'United Health',
    insuranceId: 'UHC-552-110034',
    primaryDiagnosis: 'Postpartum Depression, Anxiety',
    riskLevel: 'Medium',
    caseCount: 1,
    lastContact: '2026-06-17',
    assignedCoordinator: 'Priya Nair',
    status: 'Active',
  },
];

/* ── CASES ─────────────────────────────────────────────────────────
   Status: Draft | Open | InProgress | PendingInfo | Closed | Escalated
   Priority: Critical | High | Medium | Low
   ─────────────────────────────────────────────────────────────────── */
const CASES = [
  {
    id: 'case-001',
    caseNumber: 'CASE-2026-00001',
    patientId: 'pat-001',
    patientName: 'Sarah Johnson',
    title: 'Diabetes Management & Home Health Coordination',
    status: 'InProgress',
    priority: 'High',
    category: 'Chronic Disease Management',
    openedDate: '2026-05-10',
    targetCloseDate: '2026-07-10',
    assignedTo: 'Maria Santos',
    summary: 'Patient requires coordinated care plan for uncontrolled T2DM with A1C of 9.2. Coordinating endocrinology referral, home health aide, and nutrition counseling.',
    notes: [
      { date: '2026-06-18', author: 'Maria Santos', text: 'Patient called — blood glucose readings improving. Scheduled follow-up with Dr. Kim on June 30.' },
      { date: '2026-06-12', author: 'Maria Santos', text: 'Home health aide visit confirmed for June 15. Patient expressed concern about medication cost — referred to pharmacy assistance program.' },
    ],
    tasks: ['task-001', 'task-002'],
    team: ['Maria Santos (Coordinator)', 'Dr. Angela Kim (Endocrinology)', 'Home Health Aide: Rosa L.'],
  },
  {
    id: 'case-002',
    caseNumber: 'CASE-2026-00002',
    patientId: 'pat-002',
    patientName: 'Robert Chen',
    title: 'Post-Hospitalization COPD Transition of Care',
    status: 'Escalated',
    priority: 'Critical',
    category: 'Transition of Care',
    openedDate: '2026-06-01',
    targetCloseDate: '2026-06-30',
    assignedTo: 'Maria Santos',
    summary: '74-year-old discharged after 5-day COPD exacerbation. 30-day readmission risk HIGH. Coordinating pulmonary rehab, oxygen therapy setup, and medication reconciliation.',
    notes: [
      { date: '2026-06-20', author: 'Maria Santos', text: 'Patient unable to use incentive spirometer correctly — arranged respiratory therapy home visit.' },
      { date: '2026-06-15', author: 'Dr. James Lee', text: 'Spirometry shows FEV1 at 38%. Escalating to critical priority. Pulmonary ICU consult recommended.' },
    ],
    tasks: ['task-003'],
    team: ['Maria Santos (Coordinator)', 'Dr. James Lee (Pulmonology)', 'Respiratory Therapist: Mike P.'],
  },
  {
    id: 'case-003',
    caseNumber: 'CASE-2026-00003',
    patientId: 'pat-003',
    patientName: 'Linda Patel',
    title: 'Lupus Flare Pain Management & Rheumatology Referral',
    status: 'Open',
    priority: 'Medium',
    category: 'Specialty Referral',
    openedDate: '2026-06-08',
    targetCloseDate: '2026-08-08',
    assignedTo: 'James Rodriguez',
    summary: 'Patient experiencing active lupus flare with joint involvement. Pending rheumatology authorization and pain management specialist consult.',
    notes: [
      { date: '2026-06-15', author: 'James Rodriguez', text: 'Prior authorization submitted to Aetna for rheumatology referral. Awaiting 5–7 business day response.' },
    ],
    tasks: ['task-004'],
    team: ['James Rodriguez (Coordinator)', 'Dr. Susan Park (Rheumatology — pending)'],
  },
  {
    id: 'case-004',
    caseNumber: 'CASE-2026-00004',
    patientId: 'pat-004',
    patientName: 'Marcus Williams',
    title: 'Alzheimer\'s Home Safety Assessment & Caregiver Support',
    status: 'InProgress',
    priority: 'High',
    category: 'Behavioral & Social Support',
    openedDate: '2026-05-20',
    targetCloseDate: '2026-08-20',
    assignedTo: 'Maria Santos',
    summary: 'Coordinating home safety evaluation, caregiver training for daughter, and memory care day program enrollment for patient with moderate-stage Alzheimer\'s.',
    notes: [
      { date: '2026-06-19', author: 'Maria Santos', text: 'Home safety assessment completed. Grab bars installed, stove auto-shutoff ordered. Daughter trained on medication management.' },
    ],
    tasks: ['task-005'],
    team: ['Maria Santos (Coordinator)', 'Dr. Nina Evans (Neurology)', 'Caregiver: Diane Williams (daughter)'],
  },
  {
    id: 'case-005',
    caseNumber: 'CASE-2026-00005',
    patientId: 'pat-005',
    patientName: 'Emily Torres',
    title: 'Postpartum Mental Health Care Coordination',
    status: 'Open',
    priority: 'High',
    category: 'Mental Health',
    openedDate: '2026-06-10',
    targetCloseDate: '2026-09-10',
    assignedTo: 'Priya Nair',
    summary: 'New mother (8 weeks postpartum) screened positive on Edinburgh scale (score 16). Coordinating psychiatry and therapy, linking to community support group.',
    notes: [
      { date: '2026-06-17', author: 'Priya Nair', text: 'Patient connected with therapist Dr. Carol Lee. First appointment June 24. Husband supportive and involved.' },
    ],
    tasks: ['task-006'],
    team: ['Priya Nair (Coordinator)', 'Dr. Carol Lee (Therapist)', 'Dr. Rohan Mehta (Psychiatry)'],
  },
  {
    id: 'case-006',
    caseNumber: 'CASE-2026-00006',
    patientId: 'pat-002',
    patientName: 'Robert Chen',
    title: 'CHF Medication Adherence & Cardiology Follow-Up',
    status: 'PendingInfo',
    priority: 'High',
    category: 'Chronic Disease Management',
    openedDate: '2026-04-15',
    targetCloseDate: '2026-07-15',
    assignedTo: 'Maria Santos',
    summary: 'Patient missing doses of Furosemide — two ER visits in past 60 days. Awaiting pharmacy dispensing records and POLST document from family.',
    notes: [
      { date: '2026-06-10', author: 'Maria Santos', text: 'Requested POLST from daughter per patient consent. Pharmacy records requested — awaiting CVS response.' },
    ],
    tasks: [],
    team: ['Maria Santos (Coordinator)', 'Dr. Patricia Moore (Cardiology)'],
  },
];

/* ── TASKS ─────────────────────────────────────────────────────────
   Status: Pending | InProgress | Completed | Cancelled
   ─────────────────────────────────────────────────────────────────── */
const TASKS = [
  {
    id: 'task-001',
    title: 'Schedule endocrinology appointment for Sarah Johnson',
    caseNumber: 'CASE-2026-00001',
    patientName: 'Sarah Johnson',
    dueDate: '2026-06-25',
    priority: 'High',
    status: 'Pending',
    assignedTo: 'Maria Santos',
    overdue: false,
  },
  {
    id: 'task-002',
    title: 'Submit pharmacy assistance program application',
    caseNumber: 'CASE-2026-00001',
    patientName: 'Sarah Johnson',
    dueDate: '2026-06-20',
    priority: 'Medium',
    status: 'Pending',
    assignedTo: 'Maria Santos',
    overdue: true,  // past due
  },
  {
    id: 'task-003',
    title: 'Confirm respiratory therapy home visit — Robert Chen',
    caseNumber: 'CASE-2026-00002',
    patientName: 'Robert Chen',
    dueDate: '2026-06-23',
    priority: 'Critical',
    status: 'InProgress',
    assignedTo: 'Maria Santos',
    overdue: false,
  },
  {
    id: 'task-004',
    title: 'Follow up on Aetna prior authorization — Linda Patel',
    caseNumber: 'CASE-2026-00003',
    patientName: 'Linda Patel',
    dueDate: '2026-06-21',
    priority: 'Medium',
    status: 'Pending',
    assignedTo: 'James Rodriguez',
    overdue: false,
  },
  {
    id: 'task-005',
    title: 'Enroll Marcus Williams in memory care day program',
    caseNumber: 'CASE-2026-00004',
    patientName: 'Marcus Williams',
    dueDate: '2026-06-18',
    priority: 'High',
    status: 'Pending',
    assignedTo: 'Maria Santos',
    overdue: true,  // past due
  },
  {
    id: 'task-006',
    title: 'Send therapy intake paperwork to Emily Torres',
    caseNumber: 'CASE-2026-00005',
    patientName: 'Emily Torres',
    dueDate: '2026-06-28',
    priority: 'High',
    status: 'Pending',
    assignedTo: 'Priya Nair',
    overdue: false,
  },
];

/* ── APPOINTMENTS ──────────────────────────────────────────────────
   Type: FollowUp | Initial | Telehealth | Procedure | LabWork
   ─────────────────────────────────────────────────────────────────── */
const APPOINTMENTS = [
  {
    id: 'appt-001',
    patientName: 'Robert Chen',
    patientId: 'pat-002',
    type: 'Telehealth',
    title: 'Pulmonology Telehealth — COPD Review',
    provider: 'Dr. James Lee',
    date: '2026-06-23',
    time: '09:00 AM',
    duration: '30 min',
    location: 'Video Call (MyChart)',
    status: 'Scheduled',
    caseNumber: 'CASE-2026-00002',
  },
  {
    id: 'appt-002',
    patientName: 'Sarah Johnson',
    patientId: 'pat-001',
    type: 'FollowUp',
    title: 'Endocrinology Follow-Up — A1C Review',
    provider: 'Dr. Angela Kim',
    date: '2026-06-30',
    time: '10:30 AM',
    duration: '45 min',
    location: 'Suite 400, Northwestern Medical Pavilion',
    status: 'Confirmed',
    caseNumber: 'CASE-2026-00001',
  },
  {
    id: 'appt-003',
    patientName: 'Emily Torres',
    patientId: 'pat-005',
    type: 'Initial',
    title: 'Initial Therapy Session — Postpartum Depression',
    provider: 'Dr. Carol Lee',
    date: '2026-06-24',
    time: '02:00 PM',
    duration: '60 min',
    location: 'Mindful Healing Center, Suite 12',
    status: 'Scheduled',
    caseNumber: 'CASE-2026-00005',
  },
  {
    id: 'appt-004',
    patientName: 'Marcus Williams',
    patientId: 'pat-004',
    type: 'FollowUp',
    title: 'Neurology Follow-Up — Alzheimer\'s Assessment',
    provider: 'Dr. Nina Evans',
    date: '2026-07-02',
    time: '11:00 AM',
    duration: '45 min',
    location: 'Memory Care Clinic, Floor 3',
    status: 'Pending Confirmation',
    caseNumber: 'CASE-2026-00004',
  },
];

/* ── NOTIFICATIONS ─────────────────────────────────────────────────── */
const NOTIFICATIONS = [
  { id: 'n-001', type: 'alert',   text: 'Robert Chen — COPD case escalated to Critical. Action required.', time: '1h ago', unread: true,  caseId: 'case-002' },
  { id: 'n-002', type: 'task',    text: 'Task overdue: "Submit pharmacy assistance program application"', time: '3h ago', unread: true,  taskId: 'task-002' },
  { id: 'n-003', type: 'task',    text: 'Task overdue: "Enroll Marcus Williams in memory care program"', time: '4h ago', unread: true,  taskId: 'task-005' },
  { id: 'n-004', type: 'appt',    text: 'Reminder: Robert Chen telehealth appointment tomorrow at 9 AM', time: '6h ago', unread: false, apptId: 'appt-001' },
  { id: 'n-005', type: 'message', text: 'Dr. James Lee added a case note on CASE-2026-00002', time: '1d ago', unread: false, caseId: 'case-002' },
];

/* ── USERS (Admin screen) ──────────────────────────────────────────── */
const USERS = [
  { id: 'u-001', name: 'Maria Santos',   role: 'CareCoordinator', email: 'maria.santos@hcms.org',  department: 'Case Mgmt – Unit A', status: 'Active',   lastLogin: '2026-06-22 08:31' },
  { id: 'u-002', name: 'James Rodriguez',role: 'CareCoordinator', email: 'j.rodriguez@hcms.org',   department: 'Case Mgmt – Unit B', status: 'Active',   lastLogin: '2026-06-22 09:05' },
  { id: 'u-003', name: 'Priya Nair',     role: 'CareCoordinator', email: 'p.nair@hcms.org',        department: 'Mental Health',      status: 'Active',   lastLogin: '2026-06-21 16:44' },
  { id: 'u-004', name: 'Dr. Angela Kim', role: 'Clinician',       email: 'a.kim@hcms.org',         department: 'Endocrinology',      status: 'Active',   lastLogin: '2026-06-20 11:00' },
  { id: 'u-005', name: 'Thomas Grant',   role: 'Supervisor',      email: 't.grant@hcms.org',       department: 'Administration',     status: 'Active',   lastLogin: '2026-06-22 07:55' },
  { id: 'u-006', name: 'Laura Kim',      role: 'ReadOnly',        email: 'l.kim@hcms.org',         department: 'Quality Assurance',  status: 'Inactive', lastLogin: '2026-06-01 10:30' },
];

/* ── AUDIT LOG ─────────────────────────────────────────────────────── */
const AUDIT_LOG = [
  { id: 'a-001', ts: '2026-06-22 09:14:02', user: 'Maria Santos',    action: 'VIEW',   resource: 'Patient',     detail: 'Viewed record for MRN-2026-00002 (Robert Chen)',         category: 'read' },
  { id: 'a-002', ts: '2026-06-22 09:12:44', user: 'Maria Santos',    action: 'UPDATE', resource: 'CaseNote',    detail: 'Added note to CASE-2026-00002',                          category: 'write' },
  { id: 'a-003', ts: '2026-06-22 08:55:11', user: 'James Rodriguez', action: 'CREATE', resource: 'Task',        detail: 'Created task "Follow up on Aetna prior auth" for CASE-2026-00003', category: 'write' },
  { id: 'a-004', ts: '2026-06-22 08:31:07', user: 'Maria Santos',    action: 'LOGIN',  resource: 'Auth',        detail: 'Successful login from 192.168.1.45',                     category: 'auth' },
  { id: 'a-005', ts: '2026-06-21 17:02:33', user: 'Priya Nair',      action: 'CREATE', resource: 'Appointment', detail: 'Scheduled appt APPT-003 for Emily Torres',              category: 'write' },
  { id: 'a-006', ts: '2026-06-21 16:44:15', user: 'Priya Nair',      action: 'VIEW',   resource: 'Patient',     detail: 'Viewed record for MRN-2026-00005 (Emily Torres)',        category: 'read' },
  { id: 'a-007', ts: '2026-06-21 14:30:00', user: 'Thomas Grant',    action: 'UPDATE', resource: 'User',        detail: 'Updated role for u-006 (Laura Kim) → Inactive',          category: 'write' },
  { id: 'a-008', ts: '2026-06-21 11:20:55', user: 'System',          action: 'EXPORT', resource: 'FhirBundle',  detail: 'FHIR R4 $everything exported for MRN-2026-00001',        category: 'read' },
];

/* ── FHIR SAMPLE — Patient R4 Resource ───────────────────────────────
   This is what the GET /fhir/R4/Patient/{id}/$everything endpoint returns.
   The prototype shows this in the FHIR Explorer screen.
   ─────────────────────────────────────────────────────────────────── */
const FHIR_BUNDLE = {
  resourceType: "Bundle",
  type: "searchset",
  total: 4,
  entry: [
    {
      resource: {
        resourceType: "Patient",
        id: "pat-001",
        identifier: [{ system: "http://hcms.org/mrn", value: "MRN-2026-00001" }],
        name: [{ use: "official", family: "Johnson", given: ["Sarah"] }],
        gender: "female",
        birthDate: "1968-03-14",
        telecom: [{ system: "phone", value: "(555) 201-4432" }],
        address: [{ text: "142 Maple Street, Springfield, IL 62701" }]
      }
    },
    {
      resource: {
        resourceType: "Encounter",
        id: "case-001",
        status: "in-progress",
        class: { code: "AMB", display: "ambulatory" },
        subject: { reference: "Patient/pat-001" },
        period: { start: "2026-05-10" },
        reasonCode: [{ text: "Diabetes Management & Home Health Coordination" }]
      }
    },
    {
      resource: {
        resourceType: "Task",
        id: "task-001",
        status: "requested",
        priority: "routine",
        description: "Schedule endocrinology appointment for Sarah Johnson",
        for: { reference: "Patient/pat-001" },
        restriction: { period: { end: "2026-06-25" } }
      }
    },
    {
      resource: {
        resourceType: "Appointment",
        id: "appt-002",
        status: "booked",
        description: "Endocrinology Follow-Up — A1C Review",
        start: "2026-06-30T10:30:00",
        end: "2026-06-30T11:15:00",
        participant: [
          { actor: { reference: "Patient/pat-001" }, status: "accepted" }
        ]
      }
    }
  ]
};

/* ── FHIR IMPORT LOGS ──────────────────────────────────────────────────
   Tracks every patient imported via POST /fhir/R4/Patient.
   Maps to the FhirImportLogs DB table in the real app.
   ─────────────────────────────────────────────────────────────────── */
const FHIR_IMPORT_LOGS = [
  {
    id: 'fimp-001',
    importedAt: '2026-06-21 11:20:55',
    importedBy: 'System',
    sourceSystem: 'Epic (Northwestern Memorial)',
    externalId: 'EPIC-77203',
    patientMrn: 'MRN-2026-00001',
    patientName: 'Sarah Johnson',
    status: 'Success',
    resourceType: 'Patient',
  }
];

/* ── SAMPLE FHIR PATIENT ───────────────────────────────────────────────
   This is the pre-filled JSON shown in the Import modal's textarea.
   Represents a patient record arriving from an external EHR (e.g. Epic).
   ─────────────────────────────────────────────────────────────────── */
const SAMPLE_FHIR_PATIENT = {
  resourceType: "Patient",
  id: "ext-david-nguyen",
  identifier: [
    { system: "http://epic.com/mrn", value: "EPIC-88441" }
  ],
  name: [{ use: "official", family: "Nguyen", given: ["David", "Minh"] }],
  gender: "male",
  birthDate: "1955-08-12",
  telecom: [
    { system: "phone", value: "(555) 812-3344" },
    { system: "email", value: "david.nguyen@email.com" }
  ],
  address: [{ text: "201 Elm Street, Joliet, IL 60432" }],
  communication: [{ language: { text: "English" }, preferred: true }]
};

/* ── DEMO CREDENTIALS (shown on login screen) ──────────────────────── */
const DEMO_USERS = {
  'coordinator': { password: 'demo', user: CURRENT_USER },
  'admin':       { password: 'demo', user: { ...CURRENT_USER, name: 'Thomas Grant', initials: 'TG', role: 'Admin' } },
};
