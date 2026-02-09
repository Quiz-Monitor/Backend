using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
                Status = "WAITING",
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
                Status = "WAITING",
                StartTime = exam.StartTime,
                Rules = rules
            };
        }

        public async Task<StartExamResponseDto> StartExamAsync(int studentId, StartExamDto dto)
        {
            // Find WAITING attempt for this student and exam

            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync
                (a => a.ExamId == dto.ExamId && a.StudentId == studentId
                && a.Status == "WAITING" && a.DeletedAt == null);

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
            attempt.Status = "ACTIVE";
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


        private async Task<QuestionResponseDto> MapToQuestionResponseDto(Question question)
        {
            var choices = await _unitOfWork.Choices
                .FindAsync(c => c.QuestionId == question.QuestionId);

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
                Choices = choices.OrderBy(c => c.OrderNumber).Select(c => new ChoiceDto
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