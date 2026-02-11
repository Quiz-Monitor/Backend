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
            // Find exam by code

            var exam = await _unitOfWork.Exams.FirstOrDefaultAsync(e => e.ExamCode == dto.ExamCode && e.DeletedAt == null);

            if (exam == null) throw new InvalidOperationException("Exam not found with the provided code");

            // Validate exam is published

            if (exam.IsPublished != true) throw new InvalidOperationException("This exam is not published yet"); 
            // Validate exam hasn't ended

            if (exam.EndTime.HasValue && DateTime.UtcNow > exam.EndTime.Value) throw new InvalidOperationException("This exam has already ended");

            // Check if student already joined

            var existingAttempt = await _unitOfWork.ExamAttempts
                .FirstOrDefaultAsync(a => a.ExamId == exam.ExamId 
                && a.StudentId == studentId 
                && a.DeletedAt == null);

            if (existingAttempt != null) throw new InvalidOperationException("You have already joined this exam");
            
            // Get instructor details
            var instructor = await _unitOfWork.Users.GetByIdAsync(exam.InstructorId);
            if (instructor == null) throw new InvalidOperationException("Instructor not found");

            
            // Create WAITING attempt

            var attempt = new ExamAttempt
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                Status = "waiting",
                CheatingStatus = "clean",
                StartTime = DateTime.UtcNow, // Record when they joined
                IsGraded = false,
                TotalViolations = 0,
                TabSwitchCount = 0,
                EyeAwayCount = 0,
                ObjectDetectedCount = 0,
                MultiplePersonCount = 0

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


            // return JoinExamResponseDto

            return new JoinExamResponseDto
            {
                ExamId = exam.ExamId,
                InstructorName = instructor.FullName,
                Title = exam.Title,
                Status = "waiting",
                StartTime = exam.StartTime,
                Rules = rules
            };
        }

        public async Task<StartExamResponseDto> StartExamAsync(int studentId, StartExamDto dto)
        {
            // Find WAITING attempt for this student and exam

            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync
                (a => a.ExamId == dto.ExamId && a.StudentId == studentId
                && a.Status == "waiting" && a.DeletedAt == null);

            if (attempt == null)
            {
                throw new InvalidOperationException("You must join the exam first before starting it");
            }


            // Get exam details

            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.ExamId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // Validate exam time window

            var now = DateTime.UtcNow;
            if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            {
                throw new InvalidOperationException("Exam has not started yet");
            }

            if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            {
                throw new InvalidOperationException("Exam has already ended");
            }

            // Transition to ACTIVE
            attempt.Status = "in_progress";
            attempt.StartTime = now; // Update to actual start time
            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Get total questions count

            var totalQuestions = await _unitOfWork.Questions.FindAsync(q => q.ExamId == exam.ExamId && q.DeletedAt == null);
            var questionsList = totalQuestions.OrderBy(q => q.OrderNumber).ToList();

            if (!questionsList.Any())
            {
                throw new InvalidOperationException("This exam has no questions");
            }

            // Get first question

            var firstQuestion = questionsList.First();
            var firstQuestionDto = await MapToQuestionResponseDto(firstQuestion);

            // return StartExamResponseDto

            return new StartExamResponseDto
            {
                AttemptId = attempt.AttemptId,
                StartTime = attempt.StartTime,
                Exam = new ExamBasicInfoDto
                {
                    Title = exam.Title,
                    DurationMinutes = exam.DurationMinutes,
                    TotalQuestions = questionsList.Count
                },
                FirstQuestion = firstQuestionDto
            };
        }



        public async Task<QuestionResponseDto> GetQuestionByOrderAsync(int attemptId, int studentId, int orderNumber)
        {
            // validate if the attmept belongs to the student
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId);
            
            if (attempt == null || attempt.DeletedAt != null) throw new InvalidOperationException("Exam attempt not found");

            if (attempt.StudentId != studentId) throw new UnauthorizedAccessException("This attempt does not belong to the student");

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

            // check if the answer already exist

            var existingAnswer = await _unitOfWork.QuestionAnswers.FirstOrDefaultAsync(qa => qa.AttemptId == attemptId
                && qa.QuestionId == dto.QuestionId && qa.DeletedAt == null);

            // calc the score for MCQs

            decimal score = 0;
            bool isCorrect = false;

            if (question.QuestionType.StartsWith("MCQ"))
            {
                // gather all choices that related to that question
                var choices = await _unitOfWork.Choices.FindAsync(c => c.QuestionId == question.QuestionId);
                // gather the IDs of corrected choices
                var correctChoices = choices.Where(c => c.IsCorrect == true).Select(c => c.ChoiceId).ToList();
                // compare between selected choices that the student has selected and corrected choices
                var selectedChoicesSet = dto.SelectedChoices.OrderBy(x => x).ToList();
                var correctChoicesSet = correctChoices.OrderBy(x => x).ToList();

                isCorrect = selectedChoicesSet.SequenceEqual(correctChoicesSet);

                if (isCorrect) score = question.Points;
                else score = 0;
            }

            var selectedChoicesJson = JsonSerializer.Serialize(dto.SelectedChoices);
            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.SelectedChoices = selectedChoicesJson;
                existingAnswer.Score = question.QuestionType.StartsWith("MCQ") ? score : null;
                existingAnswer.IsCorrect = question.QuestionType.StartsWith("MCQ") ? isCorrect : null;
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
                // Insert new answer
                var newAnswer = new QuestionAnswer
                {
                    AttemptId = attemptId,
                    QuestionId = dto.QuestionId,
                    SelectedChoices = selectedChoicesJson,
                    Score = question.QuestionType.StartsWith("MCQ") ? score : null,
                    IsCorrect = question.QuestionType.StartsWith("MCQ") ? isCorrect : null,
                    StartedAt = dto.StartedAt,
                    AnsweredAt = dto.AnsweredAt,
                    TimeSpentSeconds = dto.TimeSpentSeconds,
                    ViolationCount = 0,
                    IsManuallyGraded = false
                };

                await _unitOfWork.QuestionAnswers.AddAsync(newAnswer);
                await _unitOfWork.SaveChangesAsync();

                return new SaveAnswerResponseDto
                {
                    AnswerId = newAnswer.AnswerId,
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

            // create violation event

            var violation = new ViolationEvent
            {
                AttemptId = attemptId,
                QuestionId = dto.QuestionId,
                ViolationType = dto.ViolationType,
                Description = dto.Description,
                DurationSeconds = dto.DurationSeconds,
                ScreenshotUrl = dto.ScreenshotUrl,
                Timestamp = DateTime.UtcNow,
                Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
            };

            await _unitOfWork.ViolationEvents.AddAsync(violation);

            // update attempt violation counters

            attempt.TotalViolations = (attempt.TotalViolations ?? 0) + 1;

            switch (dto.ViolationType.ToUpper())
            {
                case "TAB_SWITCH":
                    attempt.TabSwitchCount = (attempt.TabSwitchCount ?? 0) + 1;
                    break;
                case "EYE_AWAY":
                    attempt.EyeAwayCount = (attempt.EyeAwayCount ?? 0) + 1;
                    break;
                case "MULTIPLE_PERSON":
                    attempt.MultiplePersonCount = (attempt.MultiplePersonCount ?? 0) + 1;
                    break;
                case "OBJECT_DETECTED":
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

            // calc total duration
            var submitTime = DateTime.UtcNow;
            var duration = (int)(submitTime - attempt.StartTime).TotalSeconds;

            // calc MCQ score

            var answers = await _unitOfWork.QuestionAnswers.FindAsync(qa => qa.AttemptId == attemptId && qa.DeletedAt == null);
            var mcqScore = answers.Where(a => a.Score.HasValue).Sum(a => a.Score.Value);

            // update attempt
            attempt.Status = "submitted";
            attempt.SubmitTime = submitTime;
            attempt.TotalDurationSeconds = duration;
            attempt.McqScore = mcqScore;
            attempt.FinalScore = mcqScore; // Will be updated after manual grading

            _unitOfWork.ExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync();

            // Determine cheating status
            var totalViolations = attempt.TotalViolations ?? 0;
            string cheatingStatus = "flagged";

            if (totalViolations == 0) cheatingStatus = "clean";
            else if (totalViolations <= 3) cheatingStatus = "warning";
            else cheatingStatus = "flagged";

            return new SubmitExamResponseDto
            {
                Status = "submitted",
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
                Choices = choices.OrderBy(c => c.OrderNumber)
                    .Select(c => new ChoiceDto
                    {
                        ChoiceId = c.ChoiceId,
                        Text = c.ChoiceText,
                        IsCorrect = false, // Never expose correct answers to students
                        OrderNumber = c.OrderNumber  
                    }).ToList()
            };
        }
    }
}