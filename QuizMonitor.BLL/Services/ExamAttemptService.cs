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
            
            var mcqScore = answers
                .Where(a => a.Score.HasValue)
                .Sum(a => a.Score.Value);

            // Update attempt
            attempt.Status = "submitted";
            attempt.SubmitTime = submitTime;
            attempt.TotalDurationSeconds = duration;
            attempt.McqScore = mcqScore;
            attempt.FinalScore = mcqScore; // Will be updated after manual grading

            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Determine cheating status
            var totalViolations = attempt.TotalViolations ?? 0;
            string cheatingStatus = "FLAGGED";

            if (totalViolations == 0) cheatingStatus = "CLEAN";
            else if (totalViolations <= 3) cheatingStatus = "WARNING";
            else cheatingStatus = "FLAGGED";

            return new SubmitExamResponseDto
            {
                Status = "SUBMITTED",
                McqScore = mcqScore,
                ManualScore = attempt.ManualScore,
                FinalScore = attempt.FinalScore ?? 0,
                TotalViolations = totalViolations,
                CheatingStatus = cheatingStatus
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