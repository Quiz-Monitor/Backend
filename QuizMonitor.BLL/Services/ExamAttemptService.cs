using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.BLL.Services
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExamAttemptService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<JoinExamResponseDto> JoinExamAsync(int studentId, JoinExamDto dto)
        {
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || student.Role.ToLower() != "student")
            {
                throw new UnauthorizedAccessException("Only students can join exams");
            }

            // Get Exam by code
            var exam = await _unitOfWork.Exams.FirstOrDefaultAsync(e => e.ExamCode == dto.ExamCode
                && e.DeletedAt == null);

            if (exam == null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // check if the exam is published
            if (exam.IsPublished != true)
            {
                throw new InvalidOperationException("Exam is not published yet");
            }

            // check if the exam has ended

            if (exam.EndTime.HasValue && exam.EndTime.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Exam has ended");
            }

            // check if student already joined

            var existingAttempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync(ea => ea.ExamId == exam.ExamId
                && ea.StudentId == studentId && ea.DeletedAt == null);

            if (existingAttempt != null)
            {
                throw new InvalidOperationException("You have already joined this exam");
            }

            // Get instructor information
            var instructor = await _unitOfWork.Users.GetByIdAsync(exam.InstructorId);

            // Create exam attempt with status "waiting"

            var attempt = new ExamAttempt
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                Status = "waiting",
                StartTime = DateTime.UtcNow // Will be updated when exam starts
            };

            await _unitOfWork.ExamAttempts.AddAsync(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Build rules list
            var rules = new List<string>();
            if (exam.TabSwitchingDetection == true)
            {
                rules.Add("Do not switch tabs");
                if (exam.MaxTabSwitches.HasValue)
                {
                    rules.Add($"Maximum tab switches allowed: {exam.MaxTabSwitches}");
                }
            }
            if (exam.CameraRequired == true)
            {
                rules.Add("Camera access is required");
            }
            if (exam.EyeTrackingEnabled == true)
            {
                rules.Add("Keep your eyes on the screen");
                if (exam.MaxEyeAwaySeconds.HasValue)
                {
                    rules.Add($"Maximum time looking away: {exam.MaxEyeAwaySeconds} seconds");
                }
            }
            if (exam.MultiplePersonDetection == true)
            {
                rules.Add("Only one person should be visible in the camera");
            }

            return new JoinExamResponseDto
            {
                AttemptId = attempt.AttemptId,   // frontend uses this for GET questions
                ExamId = exam.ExamId,
                InstructorName = instructor?.FullName ?? "Unknown",
                Title = exam.Title,
                Status = "WAITING",
                StartTime = exam.StartTime,
                Rules = rules
            };
        }

        public async Task<StartExamResponseDto> StartExamAsync(int studentId, StartExamDto dto)
        {
            // Get attempt
            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync(ea => ea.ExamId == dto.ExamId
                && ea.StudentId == studentId && ea.DeletedAt == null);

            if (attempt == null)
            {
                throw new InvalidOperationException("You must join the exam first");
            }

            if (attempt.Status != "waiting")
            {
                throw new InvalidOperationException("Exam has already started or ended");
            }

            // Get exam
            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.ExamId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // Check time window
            var now = DateTime.UtcNow;
            if (exam.StartTime.HasValue && exam.StartTime.Value > now)
            {
                throw new InvalidOperationException("Exam has not started yet");
            }
            if (exam.EndTime.HasValue && exam.EndTime.Value < now)
            {
                throw new InvalidOperationException("Exam has ended");
            }

            // Update attempt status
            attempt.Status = "in_progress";
            attempt.StartTime = DateTime.UtcNow;
            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Get first question
            var firstQuestion = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.ExamId == exam.ExamId
                && q.OrderNumber == 1 && q.DeletedAt == null);

            if (firstQuestion == null)
            {
                throw new InvalidOperationException("No questions found for this exam");
            }

            // Get total questions count
            var totalQuestions = await _unitOfWork.Questions.CountAsync(q => q.ExamId == exam.ExamId
                && q.DeletedAt == null);

            return new StartExamResponseDto
            {
                AttemptId = attempt.AttemptId,
                StartTime = attempt.StartTime,
                Exam = new ExamBasicInfoDto
                {
                    Title = exam.Title,
                    DurationMinutes = exam.DurationMinutes,
                    TotalQuestions = totalQuestions
                },
                FirstQuestion = await MapToQuestionResponseDto(firstQuestion)
            };
        }

        public async Task<QuestionResponseDto> GetQuestionByOrderAsync(int attemptId, int studentId, int orderNumber)
        {
            // validate attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null) throw new InvalidOperationException("Exam attempt not found");
            if (attempt.StudentId != studentId) throw new UnauthorizedAccessException("This attempt does not belong to you");
            if (attempt.Status != "in_progress") throw new InvalidOperationException("Exam attempt is not active");

            var question = await _unitOfWork.Questions.FirstOrDefaultAsync(q => q.ExamId == attempt.ExamId
                && q.OrderNumber == orderNumber && q.DeletedAt == null);

            if (question == null)
            {
                throw new InvalidOperationException("Question not found");
            }

            return await MapToQuestionResponseDto(question);
        }

        public async Task<SaveAnswerResponseDto> SaveAnswerAsync(int attemptId, int studentId, SaveAnswerDto dto)
        {
            // validate attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null) throw new InvalidOperationException("Exam attempt not found");
            if (attempt.StudentId != studentId) throw new UnauthorizedAccessException("This attempt does not belong to you");
            if (attempt.Status != "in_progress") throw new InvalidOperationException("Exam attempt is not active");

            // validate question belongs to the exam
            var question = await _unitOfWork.Questions.GetByIdAsync(dto.QuestionId);
            if (question == null || question.DeletedAt != null || question.ExamId != attempt.ExamId)
            {
                throw new InvalidOperationException("Question not found or does not belong to this exam");
            }

            // check if the answer already exists
            var existingAnswer = await _unitOfWork.QuestionAnswers.FirstOrDefaultAsync(qa => qa.AttemptId == attemptId
                && qa.QuestionId == dto.QuestionId && qa.DeletedAt == null);

            // calculate score for MCQs
            decimal score = 0;
            bool isCorrect = false;

            if (question.QuestionType.ToLower().StartsWith("mcq"))
            {
                if (dto.SelectedChoices == null || !dto.SelectedChoices.Any())
                {
                    // No answer selected - score is 0
                    score = 0;
                    isCorrect = false;
                }
                else
                {
                    // gather all choices related to that question
                    var choices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == question.QuestionId);
                    // gather the IDs of correct choices
                    var correctChoices = choices.Where(c => c.IsCorrect == true).Select(c => c.ChoiceId).ToList();
                    // compare selected choices with correct choices
                    var selectedChoicesSet = dto.SelectedChoices.OrderBy(x => x).ToList();
                    var correctChoicesSet = correctChoices.OrderBy(x => x).ToList();

                    isCorrect = selectedChoicesSet.SequenceEqual(correctChoicesSet);

                    if (isCorrect) score = question.Points;
                    else score = 0;
                }
            }
            else if (question.QuestionType.ToLower() == "open_ended")
            {
                // Open-ended questions don't get auto-scored
                score = 0;
                isCorrect = false;
            }

            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.SelectedChoices = dto.SelectedChoices != null && dto.SelectedChoices.Any()
                    ? JsonSerializer.Serialize(dto.SelectedChoices)
                    : null;
                existingAnswer.AnswerText = dto.AnswerText;
                existingAnswer.Score = question.QuestionType.ToLower().StartsWith("mcq") ? score : (decimal?)null;
                existingAnswer.IsCorrect = question.QuestionType.ToLower().StartsWith("mcq") ? isCorrect : (bool?)null;
                existingAnswer.StartedAt = dto.StartedAt;
                existingAnswer.AnsweredAt = dto.AnsweredAt;
                existingAnswer.TimeSpentSeconds = dto.TimeSpentSeconds;

                _unitOfWork.QuestionAnswers.Update(existingAnswer);
                await _unitOfWork.SaveChangesAsync();

                return new SaveAnswerResponseDto
                {
                    AnswerId = existingAnswer.AnswerId,
                    IsCorrect = isCorrect,
                    Score = score
                };
            }
            else
            {
                // Create new answer
                var answer = new QuestionAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = dto.QuestionId,
                    SelectedChoices = dto.SelectedChoices != null && dto.SelectedChoices.Any()
                        ? JsonSerializer.Serialize(dto.SelectedChoices)
                        : null,
                    AnswerText = dto.AnswerText,
                    Score = question.QuestionType.ToLower().StartsWith("mcq") ? score : (decimal?)null,
                    IsCorrect = question.QuestionType.ToLower().StartsWith("mcq") ? isCorrect : (bool?)null,
                    StartedAt = dto.StartedAt,
                    AnsweredAt = dto.AnsweredAt,
                    TimeSpentSeconds = dto.TimeSpentSeconds
                };

                await _unitOfWork.QuestionAnswers.AddAsync(answer);
                await _unitOfWork.SaveChangesAsync();

                return new SaveAnswerResponseDto
                {
                    AnswerId = answer.AnswerId,
                    IsCorrect = isCorrect,
                    Score = score
                };
            }
        }

        public async Task<LogViolationResponseDto> LogViolationAsync(int attemptId, int studentId, LogViolationDto dto)
        {
            // validate attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null) throw new InvalidOperationException("Exam attempt not found");
            if (attempt.StudentId != studentId) throw new UnauthorizedAccessException("This attempt does not belong to you");
            if (attempt.Status != "in_progress") throw new InvalidOperationException("Exam attempt is not active");

            // Normalize violation type to lowercase for database
            var normalizedViolationType = dto.ViolationType.ToLower().Replace("_", "_");

            // create violation event
            var violation = new ViolationEvent
            {
                AttemptId = attemptId,
                QuestionId = dto.QuestionId,
                ViolationType = normalizedViolationType,
                Description = dto.Description,
                DurationSeconds = dto.DurationSeconds,
                ScreenshotUrl = dto.ScreenshotUrl,
                Timestamp = DateTime.UtcNow,
                Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
            };

            await _unitOfWork.ViolationEvents.AddAsync(violation);

            // update attempt violation counters
            attempt.TotalViolations = (attempt.TotalViolations ?? 0) + 1;

            switch (normalizedViolationType)
            {
                case "tab_switch":
                    attempt.TabSwitchCount = (attempt.TabSwitchCount ?? 0) + 1;
                    break;
                case "eye_away":
                    attempt.EyeAwayCount = (attempt.EyeAwayCount ?? 0) + 1;
                    break;
                case "multiple_person":
                    attempt.MultiplePersonCount = (attempt.MultiplePersonCount ?? 0) + 1;
                    break;
                case "object_detected":
                    attempt.ObjectDetectedCount = (attempt.ObjectDetectedCount ?? 0) + 1;
                    break;
            }

            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            return new LogViolationResponseDto
            {
                ViolationId = violation.ViolationId,
                TotalViolations = attempt.TotalViolations ?? 0
            };
        }

        public async Task<SubmitExamResponseDto> SubmitExamAsync(int attemptId, int studentId)
        {
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam attempt not found");
            }

            if (attempt.StudentId != studentId)
            {
                throw new UnauthorizedAccessException("This attempt does not belong to you");
            }

            if (attempt.Status != "in_progress")
            {
                throw new InvalidOperationException("Exam attempt is not active");
            }

            // Calculate total duration
            var submitTime = DateTime.UtcNow;
            var duration = (int)(submitTime - attempt.StartTime).TotalSeconds;

            // Recalculate MCQ score from all answers
            var answers = await _unitOfWork.QuestionAnswers.FindAsync(qa =>
                qa.AttemptId == attemptId && qa.DeletedAt == null);

            // Sum all auto-graded (MCQ) scores from submitted answers
            var mcqScore = answers
                .Where(a => a.Score.HasValue && a.IsManuallyGraded != true)
                .Sum(a => a.Score.Value);

            // Check whether the exam contains any questions that require manual grading
            var examQuestions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == attempt.ExamId && q.DeletedAt == null);

            bool hasManualQuestions = examQuestions.Any(
                q => !q.QuestionType.ToLower().StartsWith("mcq"));

            // Set attempt fields
            attempt.SubmitTime = submitTime;
            attempt.TotalDurationSeconds = duration;
            attempt.McqScore = mcqScore;

            if (hasManualQuestions)
            {
                // Pending: instructor must grade the open-ended questions
                attempt.Status    = "submitted";
                attempt.FinalScore = null; // calculated after manual grading completes
            }
            else
            {
                // All MCQ: auto-grade right now, student sees their score immediately
                attempt.Status    = "graded";
                attempt.FinalScore = mcqScore;
                attempt.IsGraded  = true;
                attempt.GradedAt  = submitTime;
            }

            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Determine cheating status
            // DB constraint: cheating_status IN ('clean', 'warning', 'flagged')  ← must be lowercase
            var totalViolations = attempt.TotalViolations ?? 0;
            string cheatingStatus;
            if (totalViolations == 0)      cheatingStatus = "clean";
            else if (totalViolations <= 3) cheatingStatus = "warning";
            else                           cheatingStatus = "flagged";

            // Persist cheating_status to the DB (was computed but never saved before)
            attempt.CheatingStatus = cheatingStatus;
            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            return new SubmitExamResponseDto
            {
                Status        = hasManualQuestions ? "SUBMITTED" : "GRADED",
                GradingStatus = hasManualQuestions ? "pending_manual_grading" : "auto_graded",
                McqScore      = mcqScore,
                ManualScore   = attempt.ManualScore,
                FinalScore    = attempt.FinalScore,   // null when pending, value when auto-graded
                TotalViolations = totalViolations,
                CheatingStatus = cheatingStatus.ToUpper()  // uppercase for API consumers
            };
        }

        public async Task<ExamAttemptDetailResponseDto> GetExamAttemptDetailsAsync(int attemptId, int instructorId)
        {
            // Get the exam attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam attempt not found");
            }

            // Get the exam and verify instructor owns it
            var exam = await _unitOfWork.Exams.GetByIdAsync(attempt.ExamId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You do not have access to this exam attempt");
            }

            // Get all question answers for this attempt
            var questionAnswers = await _unitOfWork.QuestionAnswers.FindAsync(qa =>
                qa.AttemptId == attemptId && qa.DeletedAt == null);

            var questionDetailsList = new List<QuestionDetailDto>();

            // Batch fetch all related data
            var questionIds = questionAnswers.Select(qa => qa.QuestionId).Distinct().ToList();
            var questions = await _unitOfWork.Questions.FindAsync(q =>
                questionIds.Contains(q.QuestionId) && q.DeletedAt == null);
            // question dictionary contains questionId as key and question object as value for quick lookup of this attempt's questions
            var questionDict = questions.ToDictionary(q => q.QuestionId);


            var answerIds = questionAnswers.Select(qa => qa.AnswerId).ToList();
            // answerViolations contains all violations related to this attempt's answers
            var allAnswerViolations = await _unitOfWork.AnswerViolations.FindAsync(
                av => answerIds.Contains(av.AnswerId));


            var violationIds = allAnswerViolations.Select(av => av.ViolationId).Distinct().ToList();
            var allViolationEvents = await _unitOfWork.ViolationEvents.FindAsync(
                v => violationIds.Contains(v.ViolationId) && v.DeletedAt == null);
            // violationDict contains violationId as key and violation event object as value for quick lookup of all violations related to this attempt's answers
            var violationDict = allViolationEvents.ToDictionary(v => v.ViolationId);

            foreach (var qa in questionAnswers)
            {
                if (!questionDict.TryGetValue(qa.QuestionId, out var question))
                {
                    continue;
                }
                // For each answer, find all related violations and gather their types
                var answerViolations = allAnswerViolations.Where(av => av.AnswerId == qa.AnswerId);
                var violationTypes = new List<string>();

                foreach (var av in answerViolations)
                {
                    if (violationDict.TryGetValue(av.ViolationId, out var violation))
                    {
                        violationTypes.Add(violation.ViolationType.ToUpper());
                    }
                }

                questionDetailsList.Add(new QuestionDetailDto
                {
                    QuestionText = question.QuestionText,
                    TimeSpentSeconds = qa.TimeSpentSeconds,
                    Violations = violationTypes
                });
            }

            // Build violation summary from attempt counters
            var violationSummary = new ViolationSummaryDto
            {
                TabSwitch = attempt.TabSwitchCount ?? 0,
                EyeAway = attempt.EyeAwayCount ?? 0,
                MultiplePersons = attempt.MultiplePersonCount ?? 0
            };

            return new ExamAttemptDetailResponseDto
            {
                Questions = questionDetailsList,
                ViolationSummary = violationSummary
            };
        }

        public async Task<ExamQuestionsResponseDto> GetAllQuestionsAsync(int attemptId, int studentId)
        {
            // Validate attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null)
                throw new InvalidOperationException("Exam attempt not found");
            if (attempt.StudentId != studentId)
                throw new UnauthorizedAccessException("This attempt does not belong to you");

            // Accept both "waiting" (first call → will start exam) and
            // "in_progress" (subsequent calls → student reconnected, just return questions)
            if (attempt.Status != "waiting" && attempt.Status != "in_progress")
                throw new InvalidOperationException("Exam has already been submitted or graded");

            // Load exam
            var exam = await _unitOfWork.Exams.GetByIdAsync(attempt.ExamId);
            if (exam == null || exam.DeletedAt != null)
                throw new InvalidOperationException("Exam not found");

            // ── START EXAM (only when still waiting) ──────────────────────────────
            if (attempt.Status == "waiting")
            {
                var now = DateTime.UtcNow;

                if (exam.StartTime.HasValue && exam.StartTime.Value > now)
                    throw new InvalidOperationException("Exam has not started yet");
                if (exam.EndTime.HasValue && exam.EndTime.Value < now)
                    throw new InvalidOperationException("Exam has ended");

                attempt.Status    = "in_progress";
                attempt.StartTime = now;
                _unitOfWork.ExamAttempts.Update(attempt);
                await _unitOfWork.SaveChangesAsync();
            }
            // ─────────────────────────────────────────────────────────────────────

            // Load all non-deleted questions ordered by their position
            var questions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == attempt.ExamId && q.DeletedAt == null);

            var questionDtos = new List<QuestionResponseDto>();
            foreach (var question in questions.OrderBy(q => q.OrderNumber))
            {
                questionDtos.Add(await MapToQuestionResponseDto(question));
            }

            return new ExamQuestionsResponseDto
            {
                AttemptId      = attempt.AttemptId,
                ExamId         = exam.ExamId,
                ExamTitle      = exam.Title,
                StartedAt      = attempt.StartTime,
                DurationMinutes = exam.DurationMinutes,
                TotalQuestions = questionDtos.Count,
                Questions      = questionDtos
            };
        }

        public async Task<BulkSaveAnswersResponseDto> BulkSaveAnswersAsync(
            int attemptId, int studentId, BulkSaveAnswersDto dto)
        {
            // Validate attempt
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            if (attempt == null || attempt.DeletedAt != null)
                throw new InvalidOperationException("Exam attempt not found");
            if (attempt.StudentId != studentId)
                throw new UnauthorizedAccessException("This attempt does not belong to you");
            if (attempt.Status != "in_progress")
                throw new InvalidOperationException("Exam attempt is not active");

            if (dto.Answers == null || !dto.Answers.Any())
                throw new InvalidOperationException("No answers provided");

            // We collect entity references so we can read the DB-generated AnswerIds
            // after the single SaveChanges call.
            var entities  = new List<QuestionAnswer>();
            var isCorrectList = new List<bool>();
            var scoreList     = new List<decimal>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var answerDto in dto.Answers)
                {
                    // Validate the question belongs to this exam
                    var question = await _unitOfWork.Questions.GetByIdAsync(answerDto.QuestionId);
                    if (question == null || question.DeletedAt != null || question.ExamId != attempt.ExamId)
                        throw new InvalidOperationException(
                            $"Question {answerDto.QuestionId} not found or does not belong to this exam");

                    // Auto-score MCQ questions
                    decimal score     = 0;
                    bool   isCorrect  = false;

                    if (question.QuestionType.ToLower().StartsWith("mcq"))
                    {
                        if (answerDto.SelectedChoices != null && answerDto.SelectedChoices.Any())
                        {
                            var choices = await _unitOfWork.Choices.FindAsync(
                                c => c.QuestionId == question.QuestionId);

                            var correctIds  = choices.Where(c => c.IsCorrect == true)
                                                     .Select(c => c.ChoiceId)
                                                     .OrderBy(x => x).ToList();
                            var selectedIds = answerDto.SelectedChoices.OrderBy(x => x).ToList();

                            isCorrect = selectedIds.SequenceEqual(correctIds);
                            if (isCorrect) score = question.Points;
                        }
                    }

                    bool isMcq = question.QuestionType.ToLower().StartsWith("mcq");

                    // Upsert: update if already exists, create otherwise
                    var existing = await _unitOfWork.QuestionAnswers.FirstOrDefaultAsync(
                        qa => qa.AttemptId == attemptId
                           && qa.QuestionId == answerDto.QuestionId
                           && qa.DeletedAt  == null);

                    if (existing != null)
                    {
                        existing.SelectedChoices = answerDto.SelectedChoices != null && answerDto.SelectedChoices.Any()
                            ? JsonSerializer.Serialize(answerDto.SelectedChoices) : null;
                        existing.AnswerText      = answerDto.AnswerText;
                        existing.Score           = isMcq ? score     : (decimal?)null;
                        existing.IsCorrect       = isMcq ? isCorrect : (bool?)null;
                        existing.StartedAt       = answerDto.StartedAt;
                        existing.AnsweredAt      = answerDto.AnsweredAt;
                        existing.TimeSpentSeconds = answerDto.TimeSpentSeconds;

                        _unitOfWork.QuestionAnswers.Update(existing);
                        entities.Add(existing);
                    }
                    else
                    {
                        var newAnswer = new QuestionAnswer
                        {
                            AttemptId  = attemptId,
                            QuestionId = answerDto.QuestionId,
                            SelectedChoices = answerDto.SelectedChoices != null && answerDto.SelectedChoices.Any()
                                ? JsonSerializer.Serialize(answerDto.SelectedChoices) : null,
                            AnswerText       = answerDto.AnswerText,
                            Score            = isMcq ? score     : (decimal?)null,
                            IsCorrect        = isMcq ? isCorrect : (bool?)null,
                            StartedAt        = answerDto.StartedAt,
                            AnsweredAt       = answerDto.AnsweredAt,
                            TimeSpentSeconds = answerDto.TimeSpentSeconds
                        };

                        await _unitOfWork.QuestionAnswers.AddAsync(newAnswer);
                        entities.Add(newAnswer);
                    }

                    isCorrectList.Add(isCorrect);
                    scoreList.Add(score);
                }

                // Single round-trip to the DB for all inserts / updates
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Build per-answer results (AnswerIds are populated by EF after SaveChanges)
            var results = entities
                .Select((entity, i) => new SaveAnswerResponseDto
                {
                    AnswerId  = entity.AnswerId,
                    IsCorrect = isCorrectList[i],
                    Score     = scoreList[i]
                })
                .ToList();

            // ── SUBMIT EXAM automatically after all answers are saved ──────────
            var submitResult = await SubmitExamAsync(attemptId, studentId);
            // ────────────────────────────────────────────────────────

            return new BulkSaveAnswersResponseDto
            {
                TotalAnswered = results.Count,
                TotalScore    = results.Sum(r => r.Score),
                Results       = results,
                SubmitResult  = submitResult
            };
        }

        private async Task<QuestionResponseDto> MapToQuestionResponseDto(Question question)
        {
            var choices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == question.QuestionId);

            // Only return choices for MCQ questions
            List<ChoiceDto>? choicesList = null;
            if (question.QuestionType.ToLower().StartsWith("mcq"))
            {
                choicesList = choices.OrderBy(c => c.OrderNumber)
                    .Select(c => new ChoiceDto
                    {
                        ChoiceId = c.ChoiceId,
                        Text = c.ChoiceText,
                        IsCorrect = false, // Never expose correct answers to students
                        OrderNumber = c.OrderNumber
                    }).ToList();
            }

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
                Choices = choicesList
            };
        }
    }
}
