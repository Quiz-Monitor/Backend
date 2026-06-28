using System;
using System.Linq;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.BLL.Services
{
    public class ExamService : IExamService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExamService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ExamResponseDto> CreateExamAsync(int instructorId, CreateExamDto dto)
        {
            // Verify instructor exists
            var instructor = await _unitOfWork.Users.GetByIdAsync(instructorId);
            if (instructor == null || instructor.Role.ToLower() != "instructor")
            {
                throw new UnauthorizedAccessException("Only instructors can create exams");
            }

            // Generate unique exam code
            var examCode = GenerateExamCode();

            var exam = new Exam
            {
                InstructorId = instructorId,
                ExamCode = examCode,
                Title = dto.Title,
                Description = dto.Description,
                DurationMinutes = dto.DurationMinutes,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CameraRequired = dto.CameraRequired ?? false,
                TabSwitchingDetection = dto.TabSwitchingDetection ?? false,
                EyeTrackingEnabled = dto.EyeTrackingEnabled ?? false,
                MultiplePersonDetection = dto.MultiplePersonDetection ?? false,
                MaxTabSwitches = dto.MaxTabSwitches,
                MaxEyeAwaySeconds = dto.MaxEyeAwaySeconds,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Exams.AddAsync(exam);
            await _unitOfWork.SaveChangesAsync();

            return MapToExamResponseDto(exam);
        }

        public async Task<ExamResponseDto> PublishExamAsync(int examId, int instructorId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to publish this exam");
            }

            if (exam.IsPublished == true)
            {
                throw new InvalidOperationException("Exam is already published");
            }

            exam.IsPublished = true;
            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync();

            return MapToExamResponseDto(exam);
        }

        public async Task<QuestionResponseDto> AddQuestionAsync(int examId, int instructorId, CreateQuestionDto dto)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this exam");
            }

            if (exam.IsPublished == true)
            {
                throw new InvalidOperationException("Cannot add questions to a published exam");
            }

            // Validate question type (database supports only 3 types)
            // Map user input to database-acceptable values: mcq_single, mcq_multiple, open_ended
            var typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "mcq_single", "mcq_single" },
                { "mcq_multiple", "mcq_multiple" },
                { "true_false", "open_ended" },
                { "short_answer", "open_ended" },
                { "essay", "open_ended" }
            };

            var inputType = dto.QuestionType.ToLower();
            if (!typeMapping.ContainsKey(inputType))
            {
                throw new InvalidOperationException($"Invalid question type. Must be one of: {string.Join(", ", typeMapping.Keys)}");
            }

            var normalizedType = typeMapping[inputType];

            // Check for duplicate order number
            var existingQuestion = await _unitOfWork.Questions.FirstOrDefaultAsync(
                q => q.ExamId == examId && q.OrderNumber == dto.OrderNumber && q.DeletedAt == null);

            if (existingQuestion != null)
            {
                throw new InvalidOperationException($"A question with order number {dto.OrderNumber} already exists in this exam");
            }

            var question = new Question
            {
                ExamId = examId,
                QuestionType = normalizedType,
                QuestionText = dto.QuestionText,
                QuestionImageUrl = dto.QuestionImageUrl,
                Points = dto.Points,
                OrderNumber = dto.OrderNumber,
                IsRequired = dto.IsRequired,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Questions.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            // The question.QuestionId should now be populated by EF Core
            var questionId = question.QuestionId;

            // Add choices if provided
            if (dto.Choices != null && dto.Choices.Any())
            {
                // Validate unique choice order numbers
                var duplicateOrders = dto.Choices
                    .GroupBy(c => c.OrderNumber)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateOrders.Any())
                {
                    throw new InvalidOperationException($"Duplicate choice order numbers found: {string.Join(", ", duplicateOrders)}");
                }

                foreach (var choiceDto in dto.Choices)
                {
                    var choice = new Choice
                    {
                        QuestionId = questionId,
                        ChoiceText = choiceDto.Text,
                        IsCorrect = choiceDto.IsCorrect,
                        OrderNumber = choiceDto.OrderNumber
                    };
                    await _unitOfWork.Choices.AddAsync(choice);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // Build response manually to avoid potential query issues
            var choices = dto.Choices?.Select(c => new ChoiceDto
            {
                Text = c.Text,
                IsCorrect = c.IsCorrect,
                OrderNumber = c.OrderNumber
            }).ToList();

            return new QuestionResponseDto
            {
                QuestionId = questionId,
                QuestionType = question.QuestionType,
                QuestionText = question.QuestionText,
                QuestionImageUrl = question.QuestionImageUrl,
                Points = question.Points,
                OrderNumber = question.OrderNumber,
                IsRequired = question.IsRequired ?? true,
                CreatedAt = question.CreatedAt,
                Choices = choices
            };
        }

        public async Task<QuestionResponseDto> UpdateQuestionAsync(int examId, int questionId, int instructorId, UpdateQuestionDto dto)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this exam");
            }

            if (exam.IsPublished == true)
            {
                throw new InvalidOperationException("Cannot update questions in a published exam");
            }

            var question = await _unitOfWork.Questions.GetByIdAsync(questionId);

            if (question == null || question.DeletedAt != null || question.ExamId != examId)
            {
                throw new InvalidOperationException("Question not found");
            }

            // Update question
            // Map question type using same mapping as AddQuestion (3 database types)
            var typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "mcq_single", "mcq_single" },
                { "mcq_multiple", "mcq_multiple" },
                { "true_false", "open_ended" },
                { "short_answer", "open_ended" },
                { "essay", "open_ended" }
            };

            var inputType = dto.QuestionType.ToLower();
            if (!typeMapping.ContainsKey(inputType))
            {
                throw new InvalidOperationException($"Invalid question type. Must be one of: {string.Join(", ", typeMapping.Keys)}");
            }

            question.QuestionType = typeMapping[inputType];
            question.QuestionText = dto.QuestionText;
            question.QuestionImageUrl = dto.QuestionImageUrl;
            question.Points = dto.Points;

            // Check for duplicate order number if changing order
            if (question.OrderNumber != dto.OrderNumber)
            {
                var existingQuestion = await _unitOfWork.Questions.FirstOrDefaultAsync(
                    q => q.ExamId == examId && q.OrderNumber == dto.OrderNumber && q.QuestionId != questionId && q.DeletedAt == null);

                if (existingQuestion != null)
                {
                    throw new InvalidOperationException($"A question with order number {dto.OrderNumber} already exists in this exam");
                }
                question.OrderNumber = dto.OrderNumber;
            }

            question.IsRequired = dto.IsRequired;

            _unitOfWork.Questions.Update(question);

            // Update choices
            if (dto.Choices != null)
            {
                // Validate unique choice order numbers
                var duplicateOrders = dto.Choices
                    .GroupBy(c => c.OrderNumber)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateOrders.Any())
                {
                    throw new InvalidOperationException($"Duplicate choice order numbers found: {string.Join(", ", duplicateOrders)}");
                }
                // Get existing choices
                var existingChoices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == questionId);

                // Remove choices that are not in the update list
                var choicesToRemove = existingChoices.Where(c =>
                    !dto.Choices.Any(dc => dc.ChoiceId == c.ChoiceId));
                _unitOfWork.Choices.DeleteRange(choicesToRemove);

                // Update or add choices
                foreach (var choiceDto in dto.Choices)
                {
                    if (choiceDto.ChoiceId.HasValue)
                    {
                        // Update existing choice
                        var existingChoice = existingChoices.FirstOrDefault(c => c.ChoiceId == choiceDto.ChoiceId.Value);
                        if (existingChoice != null)
                        {
                            existingChoice.ChoiceText = choiceDto.Text;
                            existingChoice.IsCorrect = choiceDto.IsCorrect;
                            existingChoice.OrderNumber = choiceDto.OrderNumber;
                            _unitOfWork.Choices.Update(existingChoice);
                        }
                    }
                    else
                    {
                        // Add new choice
                        var newChoice = new Choice
                        {
                            QuestionId = questionId,
                            ChoiceText = choiceDto.Text,
                            IsCorrect = choiceDto.IsCorrect,
                            OrderNumber = choiceDto.OrderNumber
                        };
                        await _unitOfWork.Choices.AddAsync(newChoice);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Get updated choices for response
            var updatedChoices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == questionId);

            return new QuestionResponseDto
            {
                QuestionId = question.QuestionId,
                QuestionType = question.QuestionType,
                QuestionText = question.QuestionText,
                QuestionImageUrl = question.QuestionImageUrl,
                Points = question.Points,
                OrderNumber = question.OrderNumber,
                IsRequired = question.IsRequired ?? true,
                CreatedAt = question.CreatedAt,
                Choices = updatedChoices.Select(c => new ChoiceDto
                {
                    ChoiceId = c.ChoiceId,
                    Text = c.ChoiceText,
                    IsCorrect = c.IsCorrect ?? false,
                    OrderNumber = c.OrderNumber
                }).ToList()
            };
        }

        public async Task<bool> RemoveQuestionAsync(int examId, int questionId, int instructorId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this exam");
            }

            if (exam.IsPublished == true)
            {
                throw new InvalidOperationException("Cannot remove questions from a published exam");
            }

            var question = await _unitOfWork.Questions.GetByIdAsync(questionId);

            if (question == null || question.DeletedAt != null || question.ExamId != examId)
            {
                throw new InvalidOperationException("Question not found");
            }

            // Soft delete
            question.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Questions.Update(question);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int instructorId)
        {
            // Validate exam exists and belongs to instructor
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view results for this exam");
            }

            // Get all exam attempts (excluding soft-deleted)
            var attempts = await _unitOfWork.ExamAttempts.FindAsync(
                ea => ea.ExamId == examId && ea.DeletedAt == null);

            // Get student IDs and fetch student info
            var studentIds = attempts.Select(a => a.StudentId).Distinct().ToList();
            var students = await _unitOfWork.Users.FindAsync(
                u => studentIds.Contains(u.UserId) && u.DeletedAt == null);

            var studentDict = students.ToDictionary(s => s.UserId, s => s.FullName);

            // Map to result DTOs
            var results = attempts.Select(attempt => new StudentExamResultDto
            {
                StudentId = attempt.StudentId,
                StudentName = studentDict.ContainsKey(attempt.StudentId)
                    ? studentDict[attempt.StudentId]
                    : "Unknown",
                FinalScore = attempt.FinalScore,
                CheatingStatus = attempt.CheatingStatus ?? "clean",
                TotalViolations = attempt.TotalViolations ?? 0
            })
            .OrderBy(r => r.StudentName)
            .ToList();

            return results;
        }

        public async Task<SubmittedStudentsResponseDto> GetSubmittedStudentsAsync(int examId, int instructorId)
        {
            // 1. Validate exam exists and belongs to instructor
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view submitted students for this exam");
            }

            // 2. Get all submitted or graded attempts (excluding soft-deleted)
            var attempts = await _unitOfWork.ExamAttempts.FindAsync(
                ea => ea.ExamId == examId
                    && ea.DeletedAt == null
                    && (ea.Status == "submitted" || ea.Status == "graded"));

            // 3. Get student info
            var studentIds = attempts.Select(a => a.StudentId).Distinct().ToList();
            var students = await _unitOfWork.Users.FindAsync(
                u => studentIds.Contains(u.UserId) && u.DeletedAt == null);
            var studentDict = students.ToDictionary(s => s.UserId, s => s);

            // 4. Get all questions for the exam to determine if written questions exist
            var allQuestions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null);

            var writtenQuestionIds = allQuestions
                .Where(q => !q.QuestionType.ToLower().StartsWith("mcq"))
                .Select(q => q.QuestionId)
                .ToHashSet();

            bool examHasWrittenQuestions = writtenQuestionIds.Count > 0;

            // 5. Build the student list
            var studentDtos = new List<SubmittedStudentDto>();

            foreach (var attempt in attempts)
            {
                var student = studentDict.ContainsKey(attempt.StudentId)
                    ? studentDict[attempt.StudentId]
                    : null;

                // Check written answers grading status for this attempt
                bool writtenAnswersGraded = false;
                if (examHasWrittenQuestions)
                {
                    var attemptAnswers = await _unitOfWork.QuestionAnswers.FindAsync(
                        qa => qa.AttemptId == attempt.AttemptId && qa.DeletedAt == null);

                    var writtenAnswers = attemptAnswers
                        .Where(a => writtenQuestionIds.Contains(a.QuestionId))
                        .ToList();

                    writtenAnswersGraded = writtenAnswers.Count > 0
                        && writtenAnswers.All(a => a.IsManuallyGraded == true && a.Score.HasValue);
                }

                studentDtos.Add(new SubmittedStudentDto
                {
                    StudentId = attempt.StudentId,
                    StudentName = student?.FullName ?? "Unknown",
                    Email = student?.Email ?? "Unknown",
                    AttemptId = attempt.AttemptId,
                    AttemptStatus = attempt.Status ?? "unknown",
                    SubmitTime = attempt.SubmitTime,
                    McqScore = attempt.McqScore,
                    ManualScore = attempt.ManualScore,
                    FinalScore = attempt.FinalScore,
                    TotalViolations = attempt.TotalViolations ?? 0,
                    CheatingStatus = attempt.CheatingStatus ?? "clean",
                    HasWrittenAnswers = examHasWrittenQuestions,
                    WrittenAnswersGraded = writtenAnswersGraded
                });
            }

            // 6. Order by student name and return
            var orderedStudents = studentDtos.OrderBy(s => s.StudentName).ToList();

            return new SubmittedStudentsResponseDto
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalSubmitted = orderedStudents.Count,
                Students = orderedStudents
            };
        }




        public async Task<List<InstructorExamDto>> GetInstructorExamsAsync(int instructorId)
        {
            // Validate instructor exists
            var instructor = await _unitOfWork.Users.GetByIdAsync(instructorId);
            if (instructor == null || instructor.Role.ToLower() != "instructor")
            {
                throw new UnauthorizedAccessException("Only instructors can view their exams");
            }

            // Get exams for instructor (excluding soft-deleted)
            var exams = await _unitOfWork.Exams.FindAsync(
                e => e.InstructorId == instructorId && e.DeletedAt == null);

            // Return empty list if no exams found
            if (exams == null || !exams.Any())
            {
                return new List<InstructorExamDto>();
            }

            var examDtos = exams.Select(e => new InstructorExamDto
            {
                ExamId = e.ExamId,
                Title = e.Title,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                DurationMinutes = e.DurationMinutes,
                ExamCode = e.ExamCode,
                IsPublished = e.IsPublished
            }).OrderByDescending(e => e.StartTime).ToList();

            return examDtos;
        }



        public async Task<InstructorExamQuestionsResponseDto> GetExamQuestionsAsync(int examId, int instructorId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this exam's questions");
            }

            // Get all non-deleted questions for this exam, ordered by OrderNumber
            var questions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null);

            var orderedQuestions = questions.OrderBy(q => q.OrderNumber).ToList();

            // Build response with choices for each question
            var questionDtos = new List<QuestionResponseDto>();
            foreach (var question in orderedQuestions)
            {
                var choices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == question.QuestionId);

                questionDtos.Add(new QuestionResponseDto
                {
                    QuestionId = question.QuestionId,
                    QuestionType = question.QuestionType,
                    QuestionText = question.QuestionText,
                    QuestionImageUrl = question.QuestionImageUrl,
                    Points = question.Points,
                    OrderNumber = question.OrderNumber,
                    IsRequired = question.IsRequired ?? true,
                    CreatedAt = question.CreatedAt,
                    Choices = choices.Select(c => new ChoiceDto
                    {
                        ChoiceId = c.ChoiceId,
                        Text = c.ChoiceText,
                        IsCorrect = c.IsCorrect ?? false,
                        OrderNumber = c.OrderNumber
                    }).OrderBy(c => c.OrderNumber).ToList()
                });
            }

            return new InstructorExamQuestionsResponseDto
            {
                ExamId = exam.ExamId,
                ExamTitle = exam.Title,
                IsPublished = exam.IsPublished ?? false,
                TotalQuestions = questionDtos.Count,
                Questions = questionDtos
            };
        }

        public async Task<ExamResponseDto> UpdateExamAsync(int examId, int instructorId, UpdateExamDto dto)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to edit this exam");
            }

            if (exam.IsPublished == true)
            {
                throw new InvalidOperationException("Cannot edit a published exam");
            }

            // Apply only non-null fields (partial update)
            if (dto.Title != null)
                exam.Title = dto.Title;
            if (dto.Description != null)
                exam.Description = dto.Description;
            if (dto.DurationMinutes.HasValue)
                exam.DurationMinutes = dto.DurationMinutes.Value;
            if (dto.StartTime.HasValue)
                exam.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue)
                exam.EndTime = dto.EndTime.Value;
            if (dto.CameraRequired.HasValue)
                exam.CameraRequired = dto.CameraRequired.Value;
            if (dto.TabSwitchingDetection.HasValue)
                exam.TabSwitchingDetection = dto.TabSwitchingDetection.Value;
            if (dto.EyeTrackingEnabled.HasValue)
                exam.EyeTrackingEnabled = dto.EyeTrackingEnabled.Value;
            if (dto.MultiplePersonDetection.HasValue)
                exam.MultiplePersonDetection = dto.MultiplePersonDetection.Value;
            if (dto.MaxTabSwitches.HasValue)
                exam.MaxTabSwitches = dto.MaxTabSwitches.Value;
            if (dto.MaxEyeAwaySeconds.HasValue)
                exam.MaxEyeAwaySeconds = dto.MaxEyeAwaySeconds.Value;

            exam.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exams.Update(exam);
            await _unitOfWork.SaveChangesAsync();

            return MapToExamResponseDto(exam);
        }

        public async Task<FullEditExamResponseDto> FullEditExamAsync(int examId, int instructorId, FullEditExamDto dto)
        {
            // ── 1. Validate exam ────────────────────────────────────────────────
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);
            if (exam == null || exam.DeletedAt != null)
                throw new InvalidOperationException("Exam not found");
            if (exam.InstructorId != instructorId)
                throw new UnauthorizedAccessException("You are not authorized to edit this exam");
            if (exam.IsPublished == true)
                throw new InvalidOperationException("Cannot edit a published exam");

            // ── 2. Validate questions upfront (before any DB write) ──────────────
            var typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "mcq_single",   "mcq_single"  },
                { "mcq_multiple", "mcq_multiple" },
                { "true_false",   "open_ended"   },
                { "short_answer", "open_ended"   },
                { "essay",        "open_ended"   }
            };

            if (dto.Questions != null && dto.Questions.Any())
            {
                var dupOrders = dto.Questions
                    .GroupBy(q => q.OrderNumber)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToList();
                if (dupOrders.Any())
                    throw new InvalidOperationException($"Duplicate question order numbers: {string.Join(", ", dupOrders)}");

                foreach (var q in dto.Questions)
                {
                    if (!typeMapping.ContainsKey(q.QuestionType))
                        throw new InvalidOperationException(
                            $"Invalid question type '{q.QuestionType}'. Must be one of: {string.Join(", ", typeMapping.Keys)}");

                    if (q.Choices != null && q.Choices.Any())
                    {
                        var dupChoiceOrders = q.Choices
                            .GroupBy(c => c.OrderNumber)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key).ToList();
                        if (dupChoiceOrders.Any())
                            throw new InvalidOperationException(
                                $"Duplicate choice order numbers in question '{q.QuestionText}': {string.Join(", ", dupChoiceOrders)}");
                    }
                }
            }

            int added = 0, updated = 0, deleted = 0;
            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // ── 3. Partial update exam metadata ────────────────────────────
                if (dto.Title != null)                  exam.Title                   = dto.Title;
                if (dto.Description != null)            exam.Description             = dto.Description;
                if (dto.DurationMinutes.HasValue)       exam.DurationMinutes         = dto.DurationMinutes.Value;
                if (dto.StartTime.HasValue)             exam.StartTime               = dto.StartTime.Value;
                if (dto.EndTime.HasValue)               exam.EndTime                 = dto.EndTime.Value;
                if (dto.CameraRequired.HasValue)        exam.CameraRequired          = dto.CameraRequired.Value;
                if (dto.TabSwitchingDetection.HasValue) exam.TabSwitchingDetection   = dto.TabSwitchingDetection.Value;
                if (dto.EyeTrackingEnabled.HasValue)    exam.EyeTrackingEnabled      = dto.EyeTrackingEnabled.Value;
                if (dto.MultiplePersonDetection.HasValue) exam.MultiplePersonDetection = dto.MultiplePersonDetection.Value;
                if (dto.MaxTabSwitches.HasValue)        exam.MaxTabSwitches          = dto.MaxTabSwitches.Value;
                if (dto.MaxEyeAwaySeconds.HasValue)     exam.MaxEyeAwaySeconds       = dto.MaxEyeAwaySeconds.Value;
                exam.UpdatedAt = now;
                _unitOfWork.Exams.Update(exam);

                // ── 4. Process questions ───────────────────────────────────────
                if (dto.Questions != null)
                {
                    var existingQuestions = (await _unitOfWork.Questions.FindAsync(
                        q => q.ExamId == examId && q.DeletedAt == null)).ToList();

                    var requestedIds = dto.Questions
                        .Where(q => q.QuestionId.HasValue)
                        .Select(q => q.QuestionId!.Value)
                        .ToHashSet();

                    // Soft-delete questions not referenced in the request
                    foreach (var eq in existingQuestions.Where(q => !requestedIds.Contains(q.QuestionId)))
                    {
                        eq.DeletedAt = now;
                        _unitOfWork.Questions.Update(eq);
                        deleted++;
                    }

                    // Collect new-question entities so we can add their choices after SaveChanges
                    var newQueueItems = new List<(Question entity, List<ChoiceDto>? choices)>();

                    foreach (var qDto in dto.Questions)
                    {
                        var normalizedType = typeMapping[qDto.QuestionType];

                        if (qDto.QuestionId.HasValue)
                        {
                            // ── UPDATE existing question ───────────────────────
                            var existing = existingQuestions.FirstOrDefault(q => q.QuestionId == qDto.QuestionId.Value);
                            if (existing == null)
                                throw new InvalidOperationException(
                                    $"Question {qDto.QuestionId} not found in this exam");

                            existing.QuestionType     = normalizedType;
                            existing.QuestionText     = qDto.QuestionText;
                            existing.QuestionImageUrl = qDto.QuestionImageUrl;
                            existing.Points           = qDto.Points;
                            existing.OrderNumber      = qDto.OrderNumber;
                            existing.IsRequired       = qDto.IsRequired;
                            _unitOfWork.Questions.Update(existing);

                            // Update choices for existing question
                            if (qDto.Choices != null)
                            {
                                var existingChoices = (await _unitOfWork.Choices.FindAsync(
                                    c => c.QuestionId == existing.QuestionId)).ToList();

                                var requestedChoiceIds = qDto.Choices
                                    .Where(c => c.ChoiceId.HasValue)
                                    .Select(c => c.ChoiceId!.Value)
                                    .ToHashSet();

                                // Remove choices not in request
                                foreach (var ec in existingChoices.Where(c => !requestedChoiceIds.Contains(c.ChoiceId)))
                                    _unitOfWork.Choices.Delete(ec);

                                foreach (var cDto in qDto.Choices)
                                {
                                    if (cDto.ChoiceId.HasValue)
                                    {
                                        var ec = existingChoices.FirstOrDefault(c => c.ChoiceId == cDto.ChoiceId.Value);
                                        if (ec != null)
                                        {
                                            ec.ChoiceText  = cDto.Text;
                                            ec.IsCorrect   = cDto.IsCorrect;
                                            ec.OrderNumber = cDto.OrderNumber;
                                            _unitOfWork.Choices.Update(ec);
                                        }
                                    }
                                    else
                                    {
                                        await _unitOfWork.Choices.AddAsync(new Choice
                                        {
                                            QuestionId = existing.QuestionId,
                                            ChoiceText = cDto.Text,
                                            IsCorrect  = cDto.IsCorrect,
                                            OrderNumber = cDto.OrderNumber
                                        });
                                    }
                                }
                            }

                            updated++;
                        }
                        else
                        {
                            // ── CREATE new question (choices added after SaveChanges) ──
                            var newQ = new Question
                            {
                                ExamId         = examId,
                                QuestionType   = normalizedType,
                                QuestionText   = qDto.QuestionText,
                                QuestionImageUrl = qDto.QuestionImageUrl,
                                Points         = qDto.Points,
                                OrderNumber    = qDto.OrderNumber,
                                IsRequired     = qDto.IsRequired,
                                CreatedAt      = now
                            };
                            await _unitOfWork.Questions.AddAsync(newQ);
                            newQueueItems.Add((newQ, qDto.Choices));
                            added++;
                        }
                    }

                    // First save: persists metadata + updates/deletes + new question shells
                    await _unitOfWork.SaveChangesAsync();

                    // Add choices for newly created questions (IDs are populated now)
                    foreach (var (newQ, choices) in newQueueItems)
                    {
                        if (choices != null && choices.Any())
                        {
                            foreach (var cDto in choices)
                            {
                                await _unitOfWork.Choices.AddAsync(new Choice
                                {
                                    QuestionId  = newQ.QuestionId,
                                    ChoiceText  = cDto.Text,
                                    IsCorrect   = cDto.IsCorrect,
                                    OrderNumber = cDto.OrderNumber
                                });
                            }
                        }
                    }

                    if (newQueueItems.Any(x => x.choices != null && x.choices.Any()))
                        await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    // Questions array omitted — only save exam metadata
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // ── 5. Build response (reload final state from DB) ─────────────────
            var finalQuestions = (await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null))
                .OrderBy(q => q.OrderNumber)
                .ToList();

            var questionDtos = new List<QuestionResponseDto>();
            foreach (var q in finalQuestions)
            {
                var choices = (await _unitOfWork.Choices.FindAsync(c => c.QuestionId == q.QuestionId))
                    .OrderBy(c => c.OrderNumber)
                    .Select(c => new ChoiceDto
                    {
                        ChoiceId    = c.ChoiceId,
                        Text        = c.ChoiceText,
                        IsCorrect   = c.IsCorrect ?? false,
                        OrderNumber = c.OrderNumber
                    }).ToList();

                questionDtos.Add(new QuestionResponseDto
                {
                    QuestionId       = q.QuestionId,
                    QuestionType     = q.QuestionType,
                    QuestionText     = q.QuestionText,
                    QuestionImageUrl = q.QuestionImageUrl,
                    Points           = q.Points,
                    OrderNumber      = q.OrderNumber,
                    IsRequired       = q.IsRequired ?? true,
                    CreatedAt        = q.CreatedAt,
                    Choices          = choices.Any() ? choices : null
                });
            }

            return new FullEditExamResponseDto
            {
                Exam      = MapToExamResponseDto(exam),
                Questions = questionDtos,
                Summary   = new FullEditSummaryDto
                {
                    QuestionsAdded   = added,
                    QuestionsUpdated = updated,
                    QuestionsDeleted = deleted,
                    TotalQuestions   = finalQuestions.Count
                }
            };
        }

        public async Task<DeleteExamResponseDto> DeleteExamAsync(int examId, int instructorId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);
            if (exam == null || exam.DeletedAt != null)
                throw new InvalidOperationException("Exam not found");
            if (exam.InstructorId != instructorId)
                throw new UnauthorizedAccessException("You are not authorized to delete this exam");

            // Guard: allow deletion only when not published, OR the exam has already ended
            var now = DateTime.UtcNow;
            if (exam.IsPublished == true && (!exam.EndTime.HasValue || exam.EndTime.Value > now))
                throw new InvalidOperationException(
                    "Cannot delete a published exam that has not ended yet");

            // Soft-delete all non-deleted questions
            var questions = (await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null)).ToList();

            foreach (var q in questions)
            {
                q.DeletedAt = now;
                _unitOfWork.Questions.Update(q);
            }

            // Soft-delete the exam
            exam.DeletedAt  = now;
            exam.DeletedBy  = instructorId;
            exam.UpdatedAt  = now;
            _unitOfWork.Exams.Update(exam);

            await _unitOfWork.SaveChangesAsync();

            return new DeleteExamResponseDto
            {
                Message          = "Exam deleted successfully",
                ExamId           = exam.ExamId,
                ExamTitle        = exam.Title,
                QuestionsDeleted = questions.Count,
                DeletedAt        = now
            };
        }

        // Helper methods
        private string GenerateExamCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private ExamResponseDto MapToExamResponseDto(Exam exam)
        {
            return new ExamResponseDto
            {
                ExamId = exam.ExamId,
                ExamCode = exam.ExamCode,
                IsPublished = exam.IsPublished ?? false,
                Title = exam.Title,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                CameraRequired = exam.CameraRequired ?? false,
                TabSwitchingDetection = exam.TabSwitchingDetection ?? false,
                EyeTrackingEnabled = exam.EyeTrackingEnabled ?? false,
                MultiplePersonDetection = exam.MultiplePersonDetection ?? false,
                MaxTabSwitches = exam.MaxTabSwitches,
                MaxEyeAwaySeconds = exam.MaxEyeAwaySeconds,
                CreatedAt = exam.CreatedAt,
                UpdatedAt = exam.UpdatedAt
            };
        }

        private async Task<QuestionResponseDto> MapToQuestionResponseDto(int questionId)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
            if (question == null)
            {
                throw new InvalidOperationException("Question not found");
            }

            var choices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == questionId);

            return new QuestionResponseDto
            {
                QuestionId = question.QuestionId,
                QuestionType = question.QuestionType,
                QuestionText = question.QuestionText,
                QuestionImageUrl = question.QuestionImageUrl,
                Points = question.Points,
                OrderNumber = question.OrderNumber,
                IsRequired = question.IsRequired ?? true,
                CreatedAt = question.CreatedAt,
                Choices = choices.Select(c => new ChoiceDto
                {
                    ChoiceId = c.ChoiceId,
                    Text = c.ChoiceText,
                    IsCorrect = c.IsCorrect ?? false,
                    OrderNumber = c.OrderNumber
                }).ToList()
            };
        }
    }
}
