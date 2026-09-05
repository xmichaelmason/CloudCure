using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CloudCure.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "allergy_reference",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allergy_reference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    before_json = table.Column<string>(type: "text", nullable: true),
                    after_json = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "body_regions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region_group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_body_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "condition_reference",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condition_reference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medication_reference",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medication_reference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    phone_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "screening_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "surgery_reference",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_reference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state_province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_addresses_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_identities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth0subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_identities_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_phone_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                    table.ForeignKey(
                        name: "fk_patients_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_members",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    specialization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    room_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    education_degree = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_members_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_roles",
                columns: table => new
                {
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    granted_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person_roles", x => new { x.person_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_person_roles_people_granted_by_person_id",
                        column: x => x.granted_by_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_person_roles_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_person_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "screening_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    screening_template_id = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    answer_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_screening_questions_screening_templates_screening_template_",
                        column: x => x.screening_template_id,
                        principalTable: "screening_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_allergies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    free_text_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_allergies", x => x.id);
                    table.CheckConstraint("ck_patient_allergies_reference_or_free_text", "reference_id IS NOT NULL OR free_text_name IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_patient_allergies_allergy_references_reference_id",
                        column: x => x.reference_id,
                        principalTable: "allergy_reference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_allergies_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_conditions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    free_text_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_conditions", x => x.id);
                    table.CheckConstraint("ck_patient_conditions_reference_or_free_text", "reference_id IS NOT NULL OR free_text_name IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_patient_conditions_condition_references_reference_id",
                        column: x => x.reference_id,
                        principalTable: "condition_reference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_conditions_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_medications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    free_text_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_medications", x => x.id);
                    table.CheckConstraint("ck_patient_medications_reference_or_free_text", "reference_id IS NOT NULL OR free_text_name IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_patient_medications_medication_references_reference_id",
                        column: x => x.reference_id,
                        principalTable: "medication_reference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_medications_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_surgeries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    free_text_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_surgeries", x => x.id);
                    table.CheckConstraint("ck_patient_surgeries_reference_or_free_text", "reference_id IS NOT NULL OR free_text_name IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_patient_surgeries_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_patient_surgeries_surgery_references_reference_id",
                        column: x => x.reference_id,
                        principalTable: "surgery_reference",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "encounters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    attending_staff_member_id = table.Column<int>(type: "integer", nullable: true),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_encounters", x => x.id);
                    table.ForeignKey(
                        name: "fk_encounters_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_encounters_staff_members_attending_staff_member_id",
                        column: x => x.attending_staff_member_id,
                        principalTable: "staff_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    encounter_id = table.Column<int>(type: "integer", nullable: false),
                    chief_complaint = table.Column<string>(type: "text", nullable: false),
                    history_of_present_illness = table.Column<string>(type: "text", nullable: false),
                    pain_scale = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                    table.CheckConstraint("ck_assessments_pain_scale", "pain_scale BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "fk_assessments_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "diagnoses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    encounter_id = table.Column<int>(type: "integer", nullable: false),
                    doctor_diagnosis_text = table.Column<string>(type: "text", nullable: true),
                    recommended_treatment = table.Column<string>(type: "text", nullable: true),
                    finalized_by_staff_member_id = table.Column<int>(type: "integer", nullable: true),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "fk_diagnoses_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_diagnoses_staff_members_finalized_by_staff_member_id",
                        column: x => x.finalized_by_staff_member_id,
                        principalTable: "staff_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "screenings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    screening_template_id = table.Column<int>(type: "integer", nullable: false),
                    patient_id = table.Column<int>(type: "integer", nullable: false),
                    encounter_id = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_by_person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screenings", x => x.id);
                    table.ForeignKey(
                        name: "fk_screenings_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_screenings_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_screenings_people_completed_by_person_id",
                        column: x => x.completed_by_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_screenings_screening_templates_screening_template_id",
                        column: x => x.screening_template_id,
                        principalTable: "screening_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vitals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    encounter_id = table.Column<int>(type: "integer", nullable: false),
                    systolic_mm_hg = table.Column<int>(type: "integer", nullable: false),
                    diastolic_mm_hg = table.Column<int>(type: "integer", nullable: false),
                    oxygen_saturation_pct = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    heart_rate_bpm = table.Column<int>(type: "integer", nullable: false),
                    respiratory_rate_bpm = table.Column<int>(type: "integer", nullable: false),
                    temperature_value = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    temperature_unit = table.Column<int>(type: "integer", nullable: false),
                    height_value = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    height_unit = table.Column<int>(type: "integer", nullable: false),
                    weight_value = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    weight_unit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vitals", x => x.id);
                    table.ForeignKey(
                        name: "fk_vitals_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_pain_points",
                columns: table => new
                {
                    assessment_id = table.Column<int>(type: "integer", nullable: false),
                    body_region_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_pain_points", x => new { x.assessment_id, x.body_region_id });
                    table.ForeignKey(
                        name: "fk_assessment_pain_points_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assessment_pain_points_body_regions_body_region_id",
                        column: x => x.body_region_id,
                        principalTable: "body_regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "screening_answers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    screening_id = table.Column<int>(type: "integer", nullable: false),
                    screening_question_id = table.Column<int>(type: "integer", nullable: false),
                    answer_bool = table.Column<bool>(type: "boolean", nullable: true),
                    answer_text = table.Column<string>(type: "text", nullable: true),
                    answer_number = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_screening_answers_screening_questions_screening_question_id",
                        column: x => x.screening_question_id,
                        principalTable: "screening_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_screening_answers_screenings_screening_id",
                        column: x => x.screening_id,
                        principalTable: "screenings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "body_regions",
                columns: new[] { "id", "code", "display_name", "region_group" },
                values: new object[,]
                {
                    { 1, "head", "Head", "Head/Neck" },
                    { 2, "face", "Face", "Head/Neck" },
                    { 3, "neck", "Neck", "Head/Neck" },
                    { 4, "chest", "Chest", "Torso" },
                    { 5, "abdomen", "Abdomen", "Torso" },
                    { 6, "pelvis", "Pelvis", "Torso" },
                    { 7, "upper_back", "Upper Back", "Back" },
                    { 8, "lower_back", "Lower Back", "Back" },
                    { 9, "shoulder_left", "Left Shoulder", "Upper Limbs" },
                    { 10, "shoulder_right", "Right Shoulder", "Upper Limbs" },
                    { 11, "arm_left", "Left Arm", "Upper Limbs" },
                    { 12, "arm_right", "Right Arm", "Upper Limbs" },
                    { 13, "hand_left", "Left Hand", "Upper Limbs" },
                    { 14, "hand_right", "Right Hand", "Upper Limbs" },
                    { 15, "hip_left", "Left Hip", "Lower Limbs" },
                    { 16, "hip_right", "Right Hip", "Lower Limbs" },
                    { 17, "leg_left", "Left Leg", "Lower Limbs" },
                    { 18, "leg_right", "Right Leg", "Lower Limbs" },
                    { 19, "foot_left", "Left Foot", "Lower Limbs" },
                    { 20, "foot_right", "Right Foot", "Lower Limbs" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (short)1, "Pending" },
                    { (short)2, "Nurse" },
                    { (short)3, "Doctor" },
                    { (short)4, "Admin" }
                });

            migrationBuilder.InsertData(
                table: "screening_templates",
                columns: new[] { "id", "code", "created_at", "is_active", "title" },
                values: new object[] { 1, "covid19_v1", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "COVID-19 Screening" });

            migrationBuilder.InsertData(
                table: "screening_questions",
                columns: new[] { "id", "answer_type", "display_order", "question_text", "screening_template_id" },
                values: new object[,]
                {
                    { 1, 1, 1, "Are you currently experiencing a fever, cough, or shortness of breath?", 1 },
                    { 2, 1, 2, "Are you currently isolating or quarantining due to a COVID-19 exposure or diagnosis?", 1 },
                    { 3, 1, 3, "Has anyone in your household experienced any COVID-19 symptoms in the last 14 days?", 1 },
                    { 4, 1, 4, "Have you tested positive for COVID-19 in the last 10 days?", 1 },
                    { 5, 1, 5, "Have you traveled outside the country in the last 14 days?", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_person_id",
                table: "addresses",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_allergy_reference_canonical_name",
                table: "allergy_reference",
                column: "canonical_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assessment_pain_points_body_region_id",
                table: "assessment_pain_points",
                column: "body_region_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_encounter_id",
                table: "assessments",
                column: "encounter_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_type_entity_id",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_identities_auth0subject",
                table: "auth_identities",
                column: "auth0subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_identities_person_id",
                table: "auth_identities",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_body_regions_code",
                table: "body_regions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_condition_reference_canonical_name",
                table: "condition_reference",
                column: "canonical_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_diagnoses_encounter_id",
                table: "diagnoses",
                column: "encounter_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_diagnoses_finalized_by_staff_member_id",
                table: "diagnoses",
                column: "finalized_by_staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_attending_staff_member_id",
                table: "encounters",
                column: "attending_staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_patient_id",
                table: "encounters",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_reference_canonical_name",
                table: "medication_reference",
                column: "canonical_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_allergies_patient_id",
                table: "patient_allergies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_allergies_reference_id",
                table: "patient_allergies",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_conditions_patient_id",
                table: "patient_conditions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_conditions_reference_id",
                table: "patient_conditions",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_medications_patient_id",
                table: "patient_medications",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_medications_reference_id",
                table: "patient_medications",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_surgeries_patient_id",
                table: "patient_surgeries",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_surgeries_reference_id",
                table: "patient_surgeries",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_person_id",
                table: "patients",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_people_email",
                table: "people",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_roles_granted_by_person_id",
                table: "person_roles",
                column: "granted_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_roles_role_id",
                table: "person_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_screening_answers_screening_id_screening_question_id",
                table: "screening_answers",
                columns: new[] { "screening_id", "screening_question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_screening_answers_screening_question_id",
                table: "screening_answers",
                column: "screening_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_screening_questions_screening_template_id",
                table: "screening_questions",
                column: "screening_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_screening_templates_code",
                table: "screening_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_screenings_completed_by_person_id",
                table: "screenings",
                column: "completed_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_screenings_encounter_id",
                table: "screenings",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "ix_screenings_patient_id",
                table: "screenings",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_screenings_screening_template_id",
                table: "screenings",
                column: "screening_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_members_person_id",
                table: "staff_members",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_members_work_email",
                table: "staff_members",
                column: "work_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_surgery_reference_canonical_name",
                table: "surgery_reference",
                column: "canonical_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vitals_encounter_id",
                table: "vitals",
                column: "encounter_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "assessment_pain_points");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "auth_identities");

            migrationBuilder.DropTable(
                name: "diagnoses");

            migrationBuilder.DropTable(
                name: "patient_allergies");

            migrationBuilder.DropTable(
                name: "patient_conditions");

            migrationBuilder.DropTable(
                name: "patient_medications");

            migrationBuilder.DropTable(
                name: "patient_surgeries");

            migrationBuilder.DropTable(
                name: "person_roles");

            migrationBuilder.DropTable(
                name: "screening_answers");

            migrationBuilder.DropTable(
                name: "vitals");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "body_regions");

            migrationBuilder.DropTable(
                name: "allergy_reference");

            migrationBuilder.DropTable(
                name: "condition_reference");

            migrationBuilder.DropTable(
                name: "medication_reference");

            migrationBuilder.DropTable(
                name: "surgery_reference");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "screening_questions");

            migrationBuilder.DropTable(
                name: "screenings");

            migrationBuilder.DropTable(
                name: "encounters");

            migrationBuilder.DropTable(
                name: "screening_templates");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "staff_members");

            migrationBuilder.DropTable(
                name: "people");
        }
    }
}
