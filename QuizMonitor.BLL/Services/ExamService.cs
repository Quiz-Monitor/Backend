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
