# 1- Authentication & Users (~3)

##  Register

`POST /api/auth/register`

**Request**

```json
{
  "email": "student1@example.com",
  "password": "P@ssw0rd",
  "fullName": "Ahmed Ali",
  "role": "Student",
  "phoneNumber": "01012345678"
}
```

**Response**

```json
{
  "userId": 12,
  "email": "student1@example.com",
  "role": "Student",
  "createdAt": "2025-01-10T12:30:00Z"
}
```

---

##  Login

`POST /api/auth/login`

**Request**

```json
{
  "email": "student1@example.com",
  "password": "P@ssw0rd"
}
```

**Response**

```json
{
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  "user": {
    "userId": 12,
    "fullName": "Ahmed Ali",
    "role": "Student"
  }
}
```

---

##  Get Profile

`GET /api/users/me`

**Response**

```json
{
  "userId": 12,
  "email": "student1@example.com",
  "fullName": "Ahmed Ali",
  "role": "Student",
  "lastLogin": "2025-01-10T13:10:00Z"
}
```

---

# 2- Exam Management (Instructor) (~ 7)

##  Get Exam Questions (for Editing)

`GET /api/exams/{examId}/questions`

**Server-side rules**

* Exam must exist and not be deleted
* Instructor must be the exam owner
* Returns all non-deleted questions with their choices (including `isCorrect`)

**Response**

```json
{
  "examId": 5,
  "examTitle": "Data Structures Quiz",
  "isPublished": false,
  "totalQuestions": 2,
  "questions": [
    {
      "questionId": 20,
      "questionType": "mcq_single",
      "questionText": "What is the time complexity of binary search?",
      "questionImageUrl": null,
      "points": 5,
      "orderNumber": 1,
      "isRequired": true,
      "createdAt": "2025-01-10T14:30:00Z",
      "updatedAt": null,
      "choices": [
        { "choiceId": 1, "text": "O(n)", "isCorrect": false, "orderNumber": 1 },
        { "choiceId": 2, "text": "O(log n)", "isCorrect": true, "orderNumber": 2 },
        { "choiceId": 3, "text": "O(n log n)", "isCorrect": false, "orderNumber": 3 }
      ]
    },
    {
      "questionId": 21,
      "questionType": "open_ended",
      "questionText": "Explain the difference between a stack and a queue.",
      "questionImageUrl": null,
      "points": 10,
      "orderNumber": 2,
      "isRequired": true,
      "createdAt": "2025-01-10T14:35:00Z",
      "updatedAt": null,
      "choices": []
    }
  ]
}
```

---

##  Edit Exam Info

`PUT /api/exams/{examId}`

**Server-side rules**

* Exam must exist and not be deleted
* Instructor must be the exam owner
* **Cannot edit a published exam** (returns 400)
* All fields are optional — only provided fields are updated (partial update)

**Request**

```json
{
  "title": "Data Structures Midterm Quiz",
  "description": "Updated midterm quiz description",
  "durationMinutes": 90,
  "startTime": "2025-02-01T10:00:00Z",
  "endTime": "2025-02-01T11:30:00Z",
  "cameraRequired": true,
  "tabSwitchingDetection": true,
  "eyeTrackingEnabled": false,
  "multiplePersonDetection": true,
  "maxTabSwitches": 3,
  "maxEyeAwaySeconds": 20
}
```

**Response**

```json
{
  "examId": 5,
  "examCode": "AB12CD",
  "isPublished": false,
  "title": "Data Structures Midterm Quiz",
  "description": "Updated midterm quiz description",
  "durationMinutes": 90,
  "startTime": "2025-02-01T10:00:00Z",
  "endTime": "2025-02-01T11:30:00Z",
  "cameraRequired": true,
  "tabSwitchingDetection": true,
  "eyeTrackingEnabled": false,
  "multiplePersonDetection": true,
  "maxTabSwitches": 3,
  "maxEyeAwaySeconds": 20,
  "createdAt": "2025-01-10T12:30:00Z",
  "updatedAt": "2025-01-11T09:00:00Z"
}
```

---

##  Create Exam


`POST /api/exams`

**Request**

```json
{
  "title": "Data Structures Quiz",
  "description": "Midterm quiz",
  "durationMinutes": 60,
  "startTime": "2025-02-01T10:00:00Z",
  "endTime": "2025-02-01T11:00:00Z",
  "cameraRequired": true,
  "tabSwitchingDetection": true,
  "eyeTrackingEnabled": true,
  "multiplePersonDetection": true,
  "maxEyeAwaySeconds": 15
}
```

**Response**

```json
{
  "examId": 5,
  "examCode": "AB12CD",
  "isPublished": false
}
```

---

##  Add Question

`POST /api/exams/{examId}/questions`

**Request (MCQ)**

```json
{
  "questionType": "MCQ_SINGLE",
  "questionText": "What is the time complexity of binary search?",
  "points": 5,
  "orderNumber": 1,
  "choices": [
    { "text": "O(n)", "isCorrect": false, "orderNumber": 1},
    { "text": "O(log n)", "isCorrect": true, "orderNumber": 2 },
    { "text": "O(n log n)", "isCorrect": false, "orderNumber": 3 }
  ]
}
```

**Response**

```json
{
  "questionId": 20,
  "questionType": "MCQ_SINGLE",
  "questionText": "What is the time complexity of binary search?",
  "points": 5,
  "orderNumber": 1,
  "createdAt": "2025-01-10T14:30:00Z"
}
```

---

##  Update Question

`PUT /api/exams/{examId}/questions/{questionId}`

**Request**

```json
{
  "questionType": "MCQ_SINGLE",
  "questionText": "What is the average time complexity of binary search?",
  "points": 10,
  "orderNumber": 1,
  "choices": [
    { "choiceId": 1, "text": "O(n)", "isCorrect": false, "orderNumber": 1},
    { "choiceId": 2, "text": "O(log n)", "isCorrect": true, "orderNumber": 2 },
    { "choiceId": 3, "text": "O(n log n)", "isCorrect": false, "orderNumber": 3 },
    { "text": "O(1)", "isCorrect": false, "orderNumber": 4 }
  ]
}
```

**Response**

```json
{
  "questionId": 20,
  "questionType": "MCQ_SINGLE",
  "questionText": "What is the average time complexity of binary search?",
  "points": 10,
  "orderNumber": 1,
  "updatedAt": "2025-01-10T15:45:00Z"
}
```

---

##  Remove Question

`DELETE /api/exams/{examId}/questions/{questionId}`

**Response**

```json
{
  "message": "Question deleted successfully",
  "questionId": 20,
  "examId": 5
}
```

---

##  Publish Exam

`POST /api/exams/{examId}/publish`

**Response**

```json
{
  "examId": 5,
  "examCode": "AB12CD",
  "status": "Published"
}
```

---

#  Exam Participation (Student) (~6)

## Join Exam by Code

**(Creates WAITING attempt, no timer yet)**

### `POST /api/exams/join`

### Request

```json
{
  "examCode": "AB12CD"
}
```

### Server-side rules

* exam exists
* `IsPublished = true`
* student not already joined
* current time ≤ exam.end_time

---

### Response

```json
{
  "examId": 5,
  "title": "Data Structures Quiz",
  "status": "WAITING",
  "startTime": "2025-02-01T10:00:00Z",
  "rules": [
    "Do not switch tabs",
    "Camera access is required"
  ]
}
```

Notes

* **No `attemptId` yet**
* Attempt row **is created** with `status = WAITING`
* Student is now *eligible* to be notified

---

## Start Exam Attempt

**(Transitions WAITING → ACTIVE)**

### `POST /api/exam-attempts/start`

### Request

```json
{
  "examId": 5
}
```

### Server-side rules

* student has WAITING attempt
* now ≥ exam.start_time
* now ≤ exam.end_time
* attempt belongs to student

---

### Response

```json
{
  "attemptId": 101,
  "startTime": "2025-02-01T10:00:00Z",
  "exam": {
    "title": "Data Structures Quiz",
    "durationMinutes": 60,
    "totalQuestions": 10
  },
  "firstQuestion": {
    "questionId": 20,
    "questionType": "MCQ_SINGLE",
    "questionText": "What is the time complexity of binary search?",
    "questionImageUrl": null,
    "points": 5,
    "orderNumber": 1,
    "choices": [
      { "choiceId": 1, "text": "O(n)", "orderNumber": 1 },
      { "choiceId": 2, "text": "O(log n)", "orderNumber": 2 },
      { "choiceId": 3, "text": "O(n log n)", "orderNumber": 3 }
    ]
  }
}
```

Notes

* Timer starts **now**
* Attempt `status = ACTIVE`
* First question returned to reduce extra call

---

## Get Question by Order

**(Sequential navigation, no preloading)**

### `GET /api/exam-attempts/{attemptId}/questions/{orderNumber}`

### Server-side rules

* attempt exists
* attempt belongs to student
* `status = ACTIVE`
* orderNumber valid

---

### Response

```json
{
  "questionId": 21,
  "questionType": "MCQ_SINGLE",
  "questionText": "Which data structure uses FIFO?",
  "points": 5,
  "orderNumber": 2,
  "choices": [
    { "choiceId": 4, "text": "Stack" },
    { "choiceId": 5, "text": "Queue" },
    { "choiceId": 6, "text": "Tree" }
  ]
}
```

Notes

* No correct answers exposed
* Order-based prevents question ID guessing

---

## Save Answer (Auto-Save)

**(Insert or update, called on every “Next”)**

### `POST /api/exam-attempts/{attemptId}/answers`

### Request

```json
{
  "questionId": 20,
  "selectedChoices": [2],
  "startedAt": "2025-02-01T10:01:00Z",
  "answeredAt": "2025-02-01T10:01:30Z",
  "timeSpentSeconds": 30
}
```

### Server-side rules

* attempt ACTIVE
* question belongs to exam
* if answer exists → update
* else → insert

---

### Response

```json
{
  "answerId": 300,
  "isCorrect": true,
  "score": 5
}
```

Notes

* Supports crash recovery
* Supports timing analytics
* Perfect for cheating correlation

---

## Log Cheating / Violation Event

**(Append-only, no updates or deletes)**

### `POST /api/exam-attempts/{attemptId}/violations`

### Request

```json
{
  "questionId": 20,
  "violationType": "TAB_SWITCH",
  "description": "User switched tab",
  "durationSeconds": 5,
  "screenshotUrl": "https://cdn/app/screenshot1.png",
  "metadata": {
    "tabCount": 1
  }
}
```

### Server-side rules

* attempt ACTIVE
* violations are immutable
* auto-increment counters

---

### Response

```json
{
  "violationId": 77,
  "totalViolations": 1
}
```

Notes

* Evidence-grade logging
* Works with instructor review
* Matches your ERD perfectly

---

## Submit Exam

**(Locks attempt, computes result)**

### `POST /api/exam-attempts/{attemptId}/submit`

### Server-side rules

* attempt ACTIVE
* auto-submit if time expired
* client timestamps ignored
* scores computed server-side

---

### Response

```json
{
  "status": "SUBMITTED",
  "mcqScore": 25,
  "manualScore": null,
  "finalScore": 25,
  "totalViolations": 2,
  "cheatingStatus": "WARNING"
}
```

Notes

* Attempt becomes immutable
* Instructor grading happens later
* Evidence preserved

---

# Final Attempt Lifecycle (Important)

```
WAITING → ACTIVE → SUBMITTED → GRADED
```
---

# 3- Grading & Results (Instructor) (~5)

##  View Exam Results

`GET /api/exams/{examId}/results`

**Response**

```json
[
  {
    "studentId": 12,
    "studentName": "Ahmed Ali",
    "finalScore": 85,
    "cheatingStatus": "Red Flag",
    "totalViolations": 3
  }
]
```

---

##  Student Detailed Report

`GET /api/exam-attempts/{attemptId}/details`

**Response**

```json
{
  "questions": [
    {
      "questionText": "Binary search complexity?",
      "timeSpentSeconds": 30,
      "violations": ["TAB_SWITCH"]
    }
  ],
  "violationSummary": {
    "tabSwitch": 2,
    "eyeAway": 1,
    "multiplePersons": 0
  }
}
```

---

##  Manual Grading (Q/A)

`POST /api/answers/{answerId}/grade`

**Request**

```json
{
  "score": 8,
  "feedback": "Good explanation"
}
```

---

##  Finalize Exam

`POST /api/exams/{examId}/finalize`

**Response**

```json
{
  "examId": 5,
  "status": "Finalized",
  "studentsNotified": true
}
```

---

# 4- Student Results

##  My Exam Results

`GET /api/students/me/results`

**Response**

```json
[
  {
    "examTitle": "Data Structures Quiz",
    "finalScore": 85,
    "cheatingStatus": "Warning"
  }
]
```

---


# 5- Notifications (~6)

## Get User Notifications

`GET /api/notifications`

**Query Parameters**
- `isRead` (optional): `true` | `false` - Filter by read status
- `limit` (optional): number - Limit results (default: 20)
- `offset` (optional): number - Pagination offset (default: 0)

**Response**

```json
{
  "notifications": [
    {
      "userNotificationId": 1,
      "notificationId": 5001,
      "notificationType": "EXAM_STARTED",
      "title": "Exam Has Started!",
      "message": "Your exam \"Data Structures Quiz\" has started. Please join now.",
      "isRead": false,
      "deliveredAt": "2025-02-01T10:00:00Z",
      "readAt": null,
      "createdAt": "2025-02-01T10:00:00Z",
      "exam": {
        "examId": 5,
        "examCode": "AB12CD",
        "title": "Data Structures Quiz"
      },
      "metadata": {
        "exam_code": "AB12CD",
        "duration_minutes": 60
      }
    },
    {
      "userNotificationId": 2,
      "notificationId": 5002,
      "notificationType": "RESULTS_PUBLISHED",
      "title": "Exam Results Available",
      "message": "Your results for \"Data Structures Quiz\" are now available. Score: 85.5/100",
      "isRead": true,
      "deliveredAt": "2025-02-01T12:00:00Z",
      "readAt": "2025-02-01T12:05:00Z",
      "createdAt": "2025-02-01T12:00:00Z",
      "exam": {
        "examId": 5,
        "title": "Data Structures Quiz"
      },
      "attempt": {
        "attemptId": 101
      },
      "metadata": {
        "final_score": 85.5,
        "status": "Clean",
        "total_violations": 0
      }
    }
  ],
  "pagination": {
    "total": 15,
    "limit": 20,
    "offset": 0,
    "hasMore": false
  },
  "unreadCount": 3
}
```

---

## Get Unread Notification Count

`GET /api/notifications/unread-count`

**Response**

```json
{
  "unreadCount": 3
}
```

---

## Mark Notification as Read

`PUT /api/notifications/{userNotificationId}/read`

**Response**

```json
{
  "userNotificationId": 1,
  "isRead": true,
  "readAt": "2025-02-01T10:05:00Z"
}
```

---

## Mark All Notifications as Read

`PUT /api/notifications/mark-all-read`

**Response**

```json
{
  "markedCount": 5,
  "message": "All notifications marked as read"
}
```

---

## Delete Notification (for user)

`DELETE /api/notifications/{userNotificationId}`

**Response**

```json
{
  "message": "Notification deleted successfully"
}
```

---

## Get Notification by ID

`GET /api/notifications/{userNotificationId}`

**Response**

```json
{
  "userNotificationId": 1,
  "notificationId": 5001,
  "notificationType": "EXAM_STARTED",
  "title": "Exam Has Started!",
  "message": "Your exam \"Data Structures Quiz\" has started. Please join now.",
  "isRead": false,
  "deliveredAt": "2025-02-01T10:00:00Z",
  "readAt": null,
  "createdAt": "2025-02-01T10:00:00Z",
  "exam": {
    "examId": 5,
    "examCode": "AB12CD",
    "title": "Data Structures Quiz"
  },
  "metadata": {
    "exam_code": "AB12CD",
    "duration_minutes": 60
  }
}
```

---

# Notification Types Reference

The system generates the following notification types:

| Type | Recipient | Trigger | Description |
|------|-----------|---------|-------------|
| `EXAM_STARTED` | Students | Exam start time reached | Notifies students that their registered exam has started |
| `EXAM_ENDING_SOON` | Students | 5 minutes before exam ends | Optional: Warns students exam is ending soon |
| `EXAM_ENDED` | Instructor | Exam end time reached | Notifies instructor that exam time is up and students can be graded |
| `RESULTS_PUBLISHED` | Students | Instructor finalizes grades | Notifies students their exam results are available |
| `MANUAL_GRADING_REQUIRED` | Instructor | Student submits exam with Q/A questions | Optional: Alerts instructor about pending manual grading |

---


#  Notification Flow Examples

## Example 1: Exam Starting (300 Students)

1. **Background Job** detects exam start time reached
2. **System creates**:
   - 1 row in `NOTIFICATION` table (notification_id: 5001)
   - 300 rows in `USER_NOTIFICATION` table (one per student)
3. **Each student calls**: `GET /api/notifications`
4. **Response**: Each gets same message but different `userNotificationId` and `isRead` status
5. **Student marks as read**: `PUT /api/notifications/1/read`
6. **Only that student's** `USER_NOTIFICATION` record is updated

## Example 2: Results Published (Different Scores)

1. **Instructor clicks** "Finalize Results"
2. **System creates**:
   - 300 rows in `NOTIFICATION` table (personalized messages with different scores)
   - 300 rows in `USER_NOTIFICATION` table
3. **Each student sees** their personalized grade notification

## Example 3: Exam Ended (Instructor)

1. **Background Job** detects exam end time reached
2. **System creates**:
   - 1 row in `NOTIFICATION` table (notification_id: 5302)
   - 1 row in `USER_NOTIFICATION` table (for instructor)
3. **Instructor receives** notification with exam statistics

