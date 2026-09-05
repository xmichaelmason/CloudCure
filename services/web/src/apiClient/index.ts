import { mintInternalToken } from "../auth/internalJwt";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

interface CallOptions {
  method?: string;
  body?: unknown;
  /** The calling person's id + roles, embedded in the internal JWT. Omit for the pre-login service call. */
  personId?: string;
  roles?: string[];
}

async function callApi<T>(path: string, options: CallOptions = {}): Promise<T> {
  const token = mintInternalToken(options.personId, options.roles ?? []);
  const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:8080";

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new ApiError(response.status, text || response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export interface ResolveIdentityResult {
  personId: string;
  roles: string[];
  status: string;
}

export function resolveIdentity(auth0Subject: string, email: string | undefined, firstName: string, lastName: string) {
  return callApi<ResolveIdentityResult>("/internal/identity/resolve", {
    method: "POST",
    body: { auth0Subject, email, firstName, lastName },
  });
}

export interface MeResult {
  personId: string;
  firstName: string;
  lastName: string;
  email: string | null;
  roles: string[];
  status: string;
}

export function getMe(personId: string, roles: string[]) {
  return callApi<MeResult>("/api/me", { personId, roles });
}

export interface RegisterStaffRequest {
  workEmail: string;
  specialization: string;
  startDate: string;
  roomNumber?: string;
  educationDegree: string;
}

export function registerStaff(personId: string, roles: string[], request: RegisterStaffRequest) {
  return callApi<void>("/api/staff", { method: "POST", personId, roles, body: request });
}

export interface PendingApproval {
  personId: string;
  firstName: string;
  lastName: string;
  email: string | null;
  requestedAt: string;
}

export function listPendingApprovals(personId: string, roles: string[]) {
  return callApi<PendingApproval[]>("/api/staff/pending-approvals", { personId, roles });
}

export function approveStaff(personId: string, roles: string[], targetPersonId: string, role: string) {
  return callApi<void>(`/api/staff/pending-approvals/${targetPersonId}/approve`, {
    method: "POST",
    personId,
    roles,
    body: { role },
  });
}

export interface PatientIntakeRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phone: string;
  email?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  stateProvince?: string;
  postalCode?: string;
  countryCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  allergies: string[];
  conditions: string[];
  medications: string[];
  surgeries: string[];
}

export interface PatientCreatedResult {
  patientId: number;
}

export function createPatient(personId: string, roles: string[], request: PatientIntakeRequest) {
  return callApi<PatientCreatedResult>("/api/patients", { method: "POST", personId, roles, body: request });
}

export interface PatientListItem {
  patientId: number;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  phoneDisplay: string | null;
}

export interface PatientListResult {
  items: PatientListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function listPatients(personId: string, roles: string[], query: { q?: string; page?: number } = {}) {
  const params = new URLSearchParams();
  if (query.q) {
    params.set("q", query.q);
  }
  if (query.page) {
    params.set("page", String(query.page));
  }
  const queryString = params.toString();
  return callApi<PatientListResult>(`/api/patients${queryString ? `?${queryString}` : ""}`, { personId, roles });
}

export interface PatientDetailResult {
  patientId: number;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  phoneDisplay: string | null;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhoneDisplay: string | null;
  allergies: string[];
  conditions: string[];
  medications: string[];
  surgeries: string[];
}

export function getPatient(personId: string, roles: string[], patientId: number | string) {
  return callApi<PatientDetailResult>(`/api/patients/${patientId}`, { personId, roles });
}

export interface ScreeningQuestion {
  id: number;
  displayOrder: number;
  questionText: string;
  answerType: "YesNo" | "Text" | "Number";
}

export interface ScreeningTemplate {
  id: number;
  code: string;
  title: string;
  questions: ScreeningQuestion[];
}

export function getScreeningTemplate(personId: string, roles: string[], code: string) {
  return callApi<ScreeningTemplate>(`/api/screening-templates/${code}`, { personId, roles });
}

export interface ScreeningAnswerInput {
  questionId: number;
  answerBool?: boolean;
  answerText?: string;
  answerNumber?: number;
}

export interface ScreeningCreatedResult {
  screeningId: number;
}

export interface EncounterCreatedResult {
  encounterId: number;
}

export function createEncounter(personId: string, roles: string[], patientId: number | string) {
  return callApi<EncounterCreatedResult>(`/api/patients/${patientId}/encounters`, { method: "POST", personId, roles, body: {} });
}

export interface VitalsInput {
  systolicMmHg: number;
  diastolicMmHg: number;
  oxygenSaturationPct: number;
  heartRateBpm: number;
  respiratoryRateBpm: number;
  temperatureValue: number;
  temperatureUnit: "Celsius" | "Fahrenheit";
  heightValue: number;
  heightUnit: "Centimeters" | "Inches";
  weightValue: number;
  weightUnit: "Kilograms" | "Pounds";
}

export function recordVitals(personId: string, roles: string[], encounterId: number | string, input: VitalsInput) {
  return callApi<void>(`/api/encounters/${encounterId}/vitals`, { method: "POST", personId, roles, body: input });
}

export interface AssessmentInput {
  chiefComplaint: string;
  historyOfPresentIllness: string;
  painScale: number;
  painBodyRegionIds: number[];
}

export function recordAssessment(personId: string, roles: string[], encounterId: number | string, input: AssessmentInput) {
  return callApi<void>(`/api/encounters/${encounterId}/assessment`, { method: "POST", personId, roles, body: input });
}

export function finalizeDiagnosis(
  personId: string,
  roles: string[],
  encounterId: number | string,
  doctorDiagnosis: string,
  recommendedTreatment: string,
) {
  return callApi<void>(`/api/encounters/${encounterId}/diagnosis`, {
    method: "POST",
    personId,
    roles,
    body: { doctorDiagnosis, recommendedTreatment },
  });
}

export function assignDoctor(personId: string, roles: string[], encounterId: number | string, staffMemberId: number) {
  return callApi<void>(`/api/encounters/${encounterId}/assign-doctor`, {
    method: "POST",
    personId,
    roles,
    body: { staffMemberId },
  });
}

export interface EncounterDetail {
  encounterId: number;
  patientId: number;
  patientName: string;
  stage: "Registered" | "VitalsPending" | "AssessmentPending" | "AwaitingDoctor" | "Finalized";
  attendingDoctorName: string | null;
  vitals: (VitalsInput & Record<string, unknown>) | null;
  assessment: { chiefComplaint: string; historyOfPresentIllness: string; painScale: number; painBodyRegions: string[] } | null;
  diagnosis: { doctorDiagnosis: string | null; recommendedTreatment: string | null; finalizedByName: string | null; finalizedAt: string | null } | null;
}

export function getEncounter(personId: string, roles: string[], encounterId: number | string) {
  return callApi<EncounterDetail>(`/api/encounters/${encounterId}`, { personId, roles });
}

export interface BodyRegion {
  id: number;
  code: string;
  displayName: string;
  regionGroup: string;
}

export function getBodyRegions(personId: string, roles: string[]) {
  return callApi<BodyRegion[]>("/api/body-regions", { personId, roles });
}

export interface StaffSearchResult {
  staffMemberId: number;
  firstName: string;
  lastName: string;
  specialization: string;
}

export function searchStaff(personId: string, roles: string[], query: { role?: string; q?: string } = {}) {
  const params = new URLSearchParams();
  if (query.role) {
    params.set("role", query.role);
  }
  if (query.q) {
    params.set("q", query.q);
  }
  const queryString = params.toString();
  return callApi<StaffSearchResult[]>(`/api/staff${queryString ? `?${queryString}` : ""}`, { personId, roles });
}

export function submitScreening(
  personId: string,
  roles: string[],
  patientId: number | string,
  templateCode: string,
  answers: ScreeningAnswerInput[],
  encounterId?: number,
) {
  return callApi<ScreeningCreatedResult>(`/api/patients/${patientId}/screenings`, {
    method: "POST",
    personId,
    roles,
    body: { templateCode, encounterId: encounterId ?? null, answers },
  });
}
