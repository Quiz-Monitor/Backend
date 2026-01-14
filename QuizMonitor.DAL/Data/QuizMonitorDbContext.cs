using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.DAL.Data;

public partial class QuizMonitorDbContext : DbContext
{
    public QuizMonitorDbContext()
    {
    }

    public QuizMonitorDbContext(DbContextOptions<QuizMonitorDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AnswerViolation> AnswerViolations { get; set; }

    public virtual DbSet<Choice> Choices { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamAttempt> ExamAttempts { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAnswer> QuestionAnswers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<ViolationEvent> ViolationEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<AnswerViolation>(entity =>
        {
            entity.HasKey(e => e.AnswerViolationId).HasName("answer_violation_pkey");

            entity.ToTable("answer_violation");

            entity.HasIndex(e => e.AnswerId, "idx_answer_violation_answer");

            entity.HasIndex(e => e.ViolationId, "idx_answer_violation_violation");

            entity.HasIndex(e => new { e.AnswerId, e.ViolationId }, "unique_answer_violation").IsUnique();

            entity.Property(e => e.AnswerViolationId).HasColumnName("answer_violation_id");
            entity.Property(e => e.AnswerId).HasColumnName("answer_id");
            entity.Property(e => e.ViolationId).HasColumnName("violation_id");

            entity.HasOne(d => d.Answer).WithMany(p => p.AnswerViolations)
                .HasForeignKey(d => d.AnswerId)
                .HasConstraintName("answer_violation_answer_id_fkey");

            entity.HasOne(d => d.Violation).WithMany(p => p.AnswerViolations)
                .HasForeignKey(d => d.ViolationId)
                .HasConstraintName("answer_violation_violation_id_fkey");
        });

        modelBuilder.Entity<Choice>(entity =>
        {
            entity.HasKey(e => e.ChoiceId).HasName("choice_pkey");

            entity.ToTable("choice");

            entity.HasIndex(e => e.QuestionId, "idx_choice_question");

            entity.HasIndex(e => new { e.QuestionId, e.OrderNumber }, "unique_choice_order").IsUnique();

            entity.Property(e => e.ChoiceId).HasColumnName("choice_id");
            entity.Property(e => e.ChoiceText).HasColumnName("choice_text");
            entity.Property(e => e.IsCorrect)
                .HasDefaultValue(false)
                .HasColumnName("is_correct");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");

            entity.HasOne(d => d.Question).WithMany(p => p.Choices)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("choice_question_id_fkey");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("exam_pkey");

            entity.ToTable("exam");

            entity.HasIndex(e => e.ExamCode, "exam_exam_code_key").IsUnique();

            entity.HasIndex(e => e.ExamCode, "idx_exam_code");

            entity.HasIndex(e => e.DeletedAt, "idx_exam_deleted_at");

            entity.HasIndex(e => e.InstructorId, "idx_exam_instructor");

            entity.HasIndex(e => e.IsPublished, "idx_exam_published");

            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.CameraRequired)
                .HasDefaultValue(true)
                .HasColumnName("camera_required");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.ExamCode)
                .HasMaxLength(20)
                .HasColumnName("exam_code");
            entity.Property(e => e.EyeTrackingEnabled)
                .HasDefaultValue(true)
                .HasColumnName("eye_tracking_enabled");
            entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(false)
                .HasColumnName("is_published");
            entity.Property(e => e.MaxEyeAwaySeconds)
                .HasDefaultValue(15)
                .HasColumnName("max_eye_away_seconds");
            entity.Property(e => e.MaxTabSwitches)
                .HasDefaultValue(3)
                .HasColumnName("max_tab_switches");
            entity.Property(e => e.MultiplePersonDetection)
                .HasDefaultValue(true)
                .HasColumnName("multiple_person_detection");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.TabSwitchingDetection)
                .HasDefaultValue(true)
                .HasColumnName("tab_switching_detection");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.ExamDeletedByNavigations)
                .HasForeignKey(d => d.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exam_deleted_by_fkey");

            entity.HasOne(d => d.Instructor).WithMany(p => p.ExamInstructors)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("exam_instructor_id_fkey");
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("exam_attempt_pkey");

            entity.ToTable("exam_attempt");

            entity.HasIndex(e => e.DeletedAt, "idx_attempt_deleted_at");

            entity.HasIndex(e => e.ExamId, "idx_attempt_exam");

            entity.HasIndex(e => e.IsGraded, "idx_attempt_graded");

            entity.HasIndex(e => e.Status, "idx_attempt_status");

            entity.HasIndex(e => e.StudentId, "idx_attempt_student");

            entity.HasIndex(e => new { e.ExamId, e.StudentId }, "unique_student_exam_attempt").IsUnique();

            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.EyeAwayCount)
                .HasDefaultValue(0)
                .HasColumnName("eye_away_count");
            entity.Property(e => e.FinalScore)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("0.0")
                .HasColumnName("final_score");
            entity.Property(e => e.GradedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("graded_at");
            entity.Property(e => e.GradedBy).HasColumnName("graded_by");
            entity.Property(e => e.InstructorNotes).HasColumnName("instructor_notes");
            entity.Property(e => e.IsGraded)
                .HasDefaultValue(false)
                .HasColumnName("is_graded");
            entity.Property(e => e.ManualScore)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("0.0")
                .HasColumnName("manual_score");
            entity.Property(e => e.McqScore)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("0.0")
                .HasColumnName("mcq_score");
            entity.Property(e => e.MultiplePersonCount)
                .HasDefaultValue(0)
                .HasColumnName("multiple_person_count");
            entity.Property(e => e.ObjectDetectedCount)
                .HasDefaultValue(0)
                .HasColumnName("object_detected_count");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'in_progress'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SubmitTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("submit_time");
            entity.Property(e => e.TabSwitchCount)
                .HasDefaultValue(0)
                .HasColumnName("tab_switch_count");
            entity.Property(e => e.TotalDurationSeconds).HasColumnName("total_duration_seconds");
            entity.Property(e => e.TotalViolations)
                .HasDefaultValue(0)
                .HasColumnName("total_violations");

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.ExamAttemptDeletedByNavigations)
                .HasForeignKey(d => d.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exam_attempt_deleted_by_fkey");

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamAttempts)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("exam_attempt_exam_id_fkey");

            entity.HasOne(d => d.GradedByNavigation).WithMany(p => p.ExamAttemptGradedByNavigations)
                .HasForeignKey(d => d.GradedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exam_attempt_graded_by_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.ExamAttemptStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("exam_attempt_student_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notification_pkey");

            entity.ToTable("notification");

            entity.HasIndex(e => e.AttemptId, "idx_notification_attempt");

            entity.HasIndex(e => e.CreatedAt, "idx_notification_created");

            entity.HasIndex(e => e.DeletedAt, "idx_notification_deleted_at");

            entity.HasIndex(e => e.ExamId, "idx_notification_exam");

            entity.HasIndex(e => e.NotificationType, "idx_notification_type");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Attempt).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notification_attempt_id_fkey");

            entity.HasOne(d => d.Exam).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notification_exam_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("question_pkey");

            entity.ToTable("question");

            entity.HasIndex(e => e.DeletedAt, "idx_question_deleted_at");

            entity.HasIndex(e => e.ExamId, "idx_question_exam");

            entity.HasIndex(e => e.QuestionType, "idx_question_type");

            entity.HasIndex(e => new { e.ExamId, e.OrderNumber }, "unique_question_order").IsUnique();

            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("is_required");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");
            entity.Property(e => e.Points)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("1.0")
                .HasColumnName("points");
            entity.Property(e => e.QuestionImageUrl).HasColumnName("question_image_url");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(20)
                .HasColumnName("question_type");

            entity.HasOne(d => d.Exam).WithMany(p => p.Questions)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("question_exam_id_fkey");
        });

        modelBuilder.Entity<QuestionAnswer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("question_answer_pkey");

            entity.ToTable("question_answer");

            entity.HasIndex(e => e.AttemptId, "idx_answer_attempt");

            entity.HasIndex(e => e.DeletedAt, "idx_answer_deleted_at");

            entity.HasIndex(e => e.QuestionId, "idx_answer_question");

            entity.HasIndex(e => new { e.AttemptId, e.QuestionId }, "unique_attempt_question").IsUnique();

            entity.Property(e => e.AnswerId).HasColumnName("answer_id");
            entity.Property(e => e.AnswerText).HasColumnName("answer_text");
            entity.Property(e => e.AnsweredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("answered_at");
            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.InstructorFeedback).HasColumnName("instructor_feedback");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.IsManuallyGraded)
                .HasDefaultValue(false)
                .HasColumnName("is_manually_graded");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Score)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("0.0")
                .HasColumnName("score");
            entity.Property(e => e.SelectedChoices).HasColumnName("selected_choices");
            entity.Property(e => e.StartedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("started_at");
            entity.Property(e => e.TimeSpentSeconds).HasColumnName("time_spent_seconds");
            entity.Property(e => e.ViolationCount)
                .HasDefaultValue(0)
                .HasColumnName("violation_count");

            entity.HasOne(d => d.Attempt).WithMany(p => p.QuestionAnswers)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("question_answer_attempt_id_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionAnswers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("question_answer_question_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pkey");

            entity.ToTable("user");

            entity.HasIndex(e => e.DeletedAt, "idx_user_deleted_at");

            entity.HasIndex(e => e.Email, "idx_user_email");

            entity.HasIndex(e => e.Role, "idx_user_role");

            entity.HasIndex(e => e.Email, "user_email_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.ProfilePicture).HasColumnName("profile_picture");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(500)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiry)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("refresh_token_expiry");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.InverseDeletedByNavigation)
                .HasForeignKey(d => d.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("user_deleted_by_fkey");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.UserNotificationId).HasName("user_notification_pkey");

            entity.ToTable("user_notification");

            entity.HasIndex(e => e.DeliveredAt, "idx_user_notification_delivered");

            entity.HasIndex(e => e.IsRead, "idx_user_notification_read");

            entity.HasIndex(e => e.UserId, "idx_user_notification_user");

            entity.HasIndex(e => new { e.NotificationId, e.UserId }, "unique_user_notification").IsUnique();

            entity.Property(e => e.UserNotificationId).HasColumnName("user_notification_id");
            entity.Property(e => e.DeliveredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("delivered_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.ReadAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("read_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("user_notification_notification_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_notification_user_id_fkey");
        });

        modelBuilder.Entity<ViolationEvent>(entity =>
        {
            entity.HasKey(e => e.ViolationId).HasName("violation_event_pkey");

            entity.ToTable("violation_event");

            entity.HasIndex(e => e.AttemptId, "idx_violation_attempt");

            entity.HasIndex(e => e.DeletedAt, "idx_violation_deleted_at");

            entity.HasIndex(e => e.QuestionId, "idx_violation_question");

            entity.HasIndex(e => e.Timestamp, "idx_violation_timestamp");

            entity.HasIndex(e => e.ViolationType, "idx_violation_type");

            entity.Property(e => e.ViolationId).HasColumnName("violation_id");
            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.ScreenshotUrl).HasColumnName("screenshot_url");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("timestamp");
            entity.Property(e => e.ViolationType)
                .HasMaxLength(50)
                .HasColumnName("violation_type");

            entity.HasOne(d => d.Attempt).WithMany(p => p.ViolationEvents)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("violation_event_attempt_id_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.ViolationEvents)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("violation_event_question_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
