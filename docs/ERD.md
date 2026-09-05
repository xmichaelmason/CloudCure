# CloudCure Database — Entity Relationship Diagram

This document maps out every piece of information CloudCure stores and how those
pieces connect to each other. It's organized by area of the app, with a diagram for
each, followed by an explanation of why it's modeled the way it is.

The database is **PostgreSQL**. Every table below was created by an EF Core migration
committed to this repo, under
`services/api/src/CloudCure.Infrastructure/Data/Migrations/`. If you want the exact,
current column list and constraints for any table, that migration history — or the
matching file in `Data/Configurations/` — is the ground truth, not this document.

---

## 1. People, patients, and staff

A **person** is any human the system knows about — name, date of birth, phone, email.
Whether that person is a *patient*, a *staff member*, or both is decided by which of
two thin tables also has a row pointing back at them, rather than by a flag or a type
column on `people` itself.

```mermaid
erDiagram
    people ||--o| addresses : "has one"
    people ||--o| patients : "is a"
    people ||--o| staff_members : "is a"
    people ||--o{ auth_identities : "logs in via"
    people ||--o{ person_roles : "holds"
    roles ||--o{ person_roles : "granted as"

    people {
        uuid id PK
        text first_name
        text last_name
        date date_of_birth "nullable"
        text phone_e164
        text email "unique, nullable"
    }
    addresses {
        int id PK
        uuid person_id FK
        text line1
        text city
        text state_province
        text postal_code
        text country_code
    }
    auth_identities {
        int id PK
        uuid person_id FK
        text auth0_subject "unique"
    }
    roles {
        smallint id PK
        text name "Pending, Nurse, Doctor, or Admin"
    }
    person_roles {
        uuid person_id FK
        smallint role_id FK
        text status "Pending, Active, or Revoked"
        uuid granted_by_person_id FK "nullable"
        timestamptz granted_at
    }
    patients {
        int id PK
        uuid person_id FK "unique"
        text emergency_contact_name
        text emergency_contact_phone_e164
    }
    staff_members {
        int id PK
        uuid person_id FK "unique"
        text work_email "unique"
        text specialization
        date start_date
        text education_degree
    }
```

**Why it's shaped this way:**

- **`person_roles` is a list, not a single column on `people`.** A person can hold more
  than one role over time, each with its own status and its own record of who granted
  it and when — so the history of "who approved this person, and as what" is never
  lost.
- **Nobody can hand themselves a role.** When someone logs in for the first time, they
  get a `person_roles` row with `status = Pending` and no real role yet. The *only* way
  that ever flips to `Active` is an admin approving them through
  `POST /api/staff/pending-approvals/{id}/approve` — it's never something a person can
  submit about themselves.
- **`date_of_birth` is nullable.** A staff account is created automatically the moment
  someone logs in via Auth0, before they've filled out any form — and Auth0 never hands
  us a birthdate, so the column has to allow "not known yet."

---

## 2. A clinical encounter (one visit)

An **encounter** is one visit — from check-in to a doctor signing off. It moves through
a fixed set of stages in order, and the API rejects any attempt to skip one (recording
an assessment before vitals exist returns an error rather than a half-finished record).

```mermaid
erDiagram
    patients ||--o{ encounters : "has"
    staff_members |o--o{ encounters : "attends"
    encounters ||--o| vitals : "has"
    encounters ||--o| assessments : "has"
    encounters ||--o| diagnoses : "has"
    assessments ||--o{ assessment_pain_points : "marks"
    body_regions ||--o{ assessment_pain_points : "is marked in"
    staff_members |o--o{ diagnoses : "finalizes"

    encounters {
        int id PK
        int patient_id FK
        int attending_staff_member_id FK "nullable"
        text stage "Registered, VitalsPending, AssessmentPending, AwaitingDoctor, or Finalized"
        timestamptz created_at
    }
    vitals {
        int id PK
        int encounter_id FK "unique"
        int systolic_mmhg
        int diastolic_mmhg
        numeric temperature_value
        text temperature_unit "Celsius or Fahrenheit"
        numeric height_value
        text height_unit "Centimeters or Inches"
        numeric weight_value
        text weight_unit "Kilograms or Pounds"
    }
    assessments {
        int id PK
        int encounter_id FK "unique"
        text chief_complaint
        text history_of_present_illness
        smallint pain_scale "0 to 10, checked by the database itself"
    }
    body_regions {
        int id PK
        text code "unique, e.g. head, chest_left"
        text display_name
        text region_group "e.g. Torso, Upper Limbs"
    }
    assessment_pain_points {
        int assessment_id FK
        int body_region_id FK
    }
    diagnoses {
        int id PK
        int encounter_id FK "unique"
        text doctor_diagnosis_text
        text recommended_treatment
        int finalized_by_staff_member_id FK
        timestamptz finalized_at
    }
```

**Why it's shaped this way:**

- **`stage` is the whole workflow, in one column.** It's the single source of truth for
  what can happen next on a given encounter, enforced server-side by
  `EncounterWorkflowService` — not something the UI infers by checking which fields
  happen to be filled in.
- **`diagnoses` is its own table, separate from `encounters`.** The visit and its
  clinical outcome are different concepts with different lifecycles, and keeping them
  apart leaves room for `finalized_by_staff_member_id` — a direct record of which doctor
  signed off, and when.
- **`assessment_pain_points` is one row per body part**, each pointing at a real
  `body_regions` entry, rather than a single delimited string. Adding a new selectable
  body region is a data change, not a code change.

---

## 3. Screening questionnaires (e.g., COVID-19 check-in)

```mermaid
erDiagram
    screening_templates ||--o{ screening_questions : "asks"
    screening_templates ||--o{ screenings : "answered as"
    patients ||--o{ screenings : "completes"
    encounters |o--o{ screenings : "tied to (optional)"
    screenings ||--o{ screening_answers : "contains"
    screening_questions ||--o{ screening_answers : "answered by"

    screening_templates {
        int id PK
        text code "unique, e.g. covid19_v1"
        text title
        bool is_active
    }
    screening_questions {
        int id PK
        int screening_template_id FK
        int display_order
        text question_text
        text answer_type "YesNo, Text, or Number"
    }
    screenings {
        int id PK
        int screening_template_id FK
        int patient_id FK
        int encounter_id FK "nullable"
        timestamptz completed_at
        uuid completed_by_person_id FK
    }
    screening_answers {
        int id PK
        int screening_id FK
        int screening_question_id FK
        bool answer_bool "nullable"
        text answer_text "nullable"
        numeric answer_number "nullable"
    }
```

**Why it's shaped this way:** a questionnaire is just rows in `screening_questions`,
each tied to a `screening_templates` row by `code` (e.g. `covid19_v1`). Adding a new
question, reordering questions, or standing up an entirely new questionnaire for
something other than COVID is a data change — no code changes, and no risk of the
question text living in more than one place and drifting out of sync with itself.

---

## 4. Allergies, conditions, medications, and surgeries

These four are all shaped the same way, so here's one of them (allergies) as the
pattern — conditions, medications, and surgeries are identical, just swap the name.

```mermaid
erDiagram
    allergy_reference ||--o{ patient_allergies : "standardizes"
    patients ||--o{ patient_allergies : "has"

    allergy_reference {
        int id PK
        text canonical_name "unique, e.g. Penicillin"
        text_array aliases "e.g. PCN, penicillin"
    }
    patient_allergies {
        int id PK
        int patient_id FK
        int reference_id FK "nullable"
        text free_text_name "nullable"
        timestamptz recorded_at
        bool is_active
        timestamptz resolved_at "nullable"
    }
```

**Why it's shaped this way:**

- **A reference table means "Penicillin" is one thing, not several.** Rather than
  storing whatever a nurse happened to type, `patient_allergies` can point at one
  canonical `allergy_reference` row that carries known aliases (like "PCN") — so the
  same real-world concept is always recognizable as the same concept, however it was
  entered.
- **`reference_id` OR `free_text_name` — never neither.** A database `CHECK` constraint
  enforces this directly. If something isn't in the reference list yet, it's still
  saved as free text rather than rejected, so the reference list can grow over time
  without ever blocking data entry.
- **`is_active` and `resolved_at` track whether something is current or historical** —
  a condition or medication doesn't just sit there with no sense of whether it still
  applies.

---

## 5. The audit trail

```mermaid
erDiagram
    audit_log {
        bigint id PK
        timestamptz occurred_at
        uuid actor_person_id "who did it, nullable for system actions"
        text entity_type "e.g. Patient, Encounter, PersonRole"
        text entity_id "the row's own id, as text"
        text action "Insert, Update, or Delete"
        jsonb before_json "nullable"
        jsonb after_json "nullable"
        text correlation_id "ties back to a request trace"
    }
```

`audit_log` deliberately has **no foreign keys**. `entity_type` + `entity_id` just name
a row in some other table as plain text, on purpose, so the audit record survives even
if that row is later deleted.

Every write to a person, patient, staff member, role grant, encounter, vitals,
assessment, diagnosis, screening, or clinical-history item automatically produces one of
these rows — in the *same* database transaction as the change itself, via an EF Core
interceptor (`CloudCureDbContext.SaveChangesAsync`). An audit row can never exist
without the change it describes, or vice versa.

---

## All 26 tables, if you just want the list

`people`, `addresses`, `auth_identities`, `roles`, `person_roles`, `patients`,
`staff_members`, `encounters`, `diagnoses`, `vitals`, `assessments`, `body_regions`,
`assessment_pain_points`, `screening_templates`, `screening_questions`, `screenings`,
`screening_answers`, `allergy_reference`, `condition_reference`, `medication_reference`,
`surgery_reference`, `patient_allergies`, `patient_conditions`, `patient_medications`,
`patient_surgeries`, `audit_log`.
