<a name='assembly'></a>
# JetFlow.Abstractions

## Contents

- [ActivityExecutionRequest](#T-JetFlow-ActivityExecutionRequest 'JetFlow.ActivityExecutionRequest')
  - [#ctor()](#M-JetFlow-ActivityExecutionRequest-#ctor 'JetFlow.ActivityExecutionRequest.#ctor')
  - [Retries](#P-JetFlow-ActivityExecutionRequest-Retries 'JetFlow.ActivityExecutionRequest.Retries')
  - [Timeouts](#P-JetFlow-ActivityExecutionRequest-Timeouts 'JetFlow.ActivityExecutionRequest.Timeouts')
- [ActivityExecutionRequest\`1](#T-JetFlow-ActivityExecutionRequest`1 'JetFlow.ActivityExecutionRequest`1')
  - [#ctor(Input)](#M-JetFlow-ActivityExecutionRequest`1-#ctor-`0- 'JetFlow.ActivityExecutionRequest`1.#ctor(`0)')
  - [Input](#P-JetFlow-ActivityExecutionRequest`1-Input 'JetFlow.ActivityExecutionRequest`1.Input')
- [ActivityResult](#T-JetFlow-ActivityResult 'JetFlow.ActivityResult')
  - [#ctor(Index,Status,ErrorMessage)](#M-JetFlow-ActivityResult-#ctor-System-UInt64,JetFlow-ActivityResultStatus,System-String- 'JetFlow.ActivityResult.#ctor(System.UInt64,JetFlow.ActivityResultStatus,System.String)')
  - [ErrorMessage](#P-JetFlow-ActivityResult-ErrorMessage 'JetFlow.ActivityResult.ErrorMessage')
  - [Index](#P-JetFlow-ActivityResult-Index 'JetFlow.ActivityResult.Index')
  - [Status](#P-JetFlow-ActivityResult-Status 'JetFlow.ActivityResult.Status')
- [ActivityResultStatus](#T-JetFlow-ActivityResultStatus 'JetFlow.ActivityResultStatus')
  - [Failure](#F-JetFlow-ActivityResultStatus-Failure 'JetFlow.ActivityResultStatus.Failure')
  - [Success](#F-JetFlow-ActivityResultStatus-Success 'JetFlow.ActivityResultStatus.Success')
  - [Timeout](#F-JetFlow-ActivityResultStatus-Timeout 'JetFlow.ActivityResultStatus.Timeout')
- [ActivityResult\`1](#T-JetFlow-ActivityResult`1 'JetFlow.ActivityResult`1')
  - [#ctor(Index,Status,ErrorMessage,Output)](#M-JetFlow-ActivityResult`1-#ctor-System-UInt64,JetFlow-ActivityResultStatus,System-String,`0- 'JetFlow.ActivityResult`1.#ctor(System.UInt64,JetFlow.ActivityResultStatus,System.String,`0)')
  - [Output](#P-JetFlow-ActivityResult`1-Output 'JetFlow.ActivityResult`1.Output')
- [ActivityRetryConfiguration](#T-JetFlow-ActivityRetryConfiguration 'JetFlow.ActivityRetryConfiguration')
  - [#ctor(MaximumAttempts,DelayBetween,RetryOnTimeout,RetryOnError,BlockedErrors)](#M-JetFlow-ActivityRetryConfiguration-#ctor-System-UInt16,System-Nullable{System-TimeSpan},System-Boolean,System-Boolean,System-String[]- 'JetFlow.ActivityRetryConfiguration.#ctor(System.UInt16,System.Nullable{System.TimeSpan},System.Boolean,System.Boolean,System.String[])')
  - [BlockedErrors](#P-JetFlow-ActivityRetryConfiguration-BlockedErrors 'JetFlow.ActivityRetryConfiguration.BlockedErrors')
  - [DelayBetween](#P-JetFlow-ActivityRetryConfiguration-DelayBetween 'JetFlow.ActivityRetryConfiguration.DelayBetween')
  - [MaximumAttempts](#P-JetFlow-ActivityRetryConfiguration-MaximumAttempts 'JetFlow.ActivityRetryConfiguration.MaximumAttempts')
  - [RetryOnError](#P-JetFlow-ActivityRetryConfiguration-RetryOnError 'JetFlow.ActivityRetryConfiguration.RetryOnError')
  - [RetryOnTimeout](#P-JetFlow-ActivityRetryConfiguration-RetryOnTimeout 'JetFlow.ActivityRetryConfiguration.RetryOnTimeout')
- [ActivityTimeoutConfiguration](#T-JetFlow-ActivityTimeoutConfiguration 'JetFlow.ActivityTimeoutConfiguration')
  - [#ctor(OverallTimeout,AttemptTimeout)](#M-JetFlow-ActivityTimeoutConfiguration-#ctor-System-Nullable{System-TimeSpan},System-Nullable{System-TimeSpan}- 'JetFlow.ActivityTimeoutConfiguration.#ctor(System.Nullable{System.TimeSpan},System.Nullable{System.TimeSpan})')
  - [AttemptTimeout](#P-JetFlow-ActivityTimeoutConfiguration-AttemptTimeout 'JetFlow.ActivityTimeoutConfiguration.AttemptTimeout')
  - [OverallTimeout](#P-JetFlow-ActivityTimeoutConfiguration-OverallTimeout 'JetFlow.ActivityTimeoutConfiguration.OverallTimeout')
- [ArchivedWorkflow](#T-JetFlow-ArchivedWorkflow 'JetFlow.ArchivedWorkflow')
  - [#ctor(ID,SchedulerId,Name,Options,StartedAt,FinishedAt,IsSuccessful,ErrorMessage,Arguments,Steps)](#M-JetFlow-ArchivedWorkflow-#ctor-System-Guid,System-Nullable{System-Guid},System-String,JetFlow-Configs-WorkflowOptions,System-DateTimeOffset,System-DateTimeOffset,System-Boolean,System-String,System-Object,JetFlow-WorkflowStep[]- 'JetFlow.ArchivedWorkflow.#ctor(System.Guid,System.Nullable{System.Guid},System.String,JetFlow.Configs.WorkflowOptions,System.DateTimeOffset,System.DateTimeOffset,System.Boolean,System.String,System.Object,JetFlow.WorkflowStep[])')
  - [Arguments](#P-JetFlow-ArchivedWorkflow-Arguments 'JetFlow.ArchivedWorkflow.Arguments')
  - [ErrorMessage](#P-JetFlow-ArchivedWorkflow-ErrorMessage 'JetFlow.ArchivedWorkflow.ErrorMessage')
  - [FinishedAt](#P-JetFlow-ArchivedWorkflow-FinishedAt 'JetFlow.ArchivedWorkflow.FinishedAt')
  - [ID](#P-JetFlow-ArchivedWorkflow-ID 'JetFlow.ArchivedWorkflow.ID')
  - [IsSuccessful](#P-JetFlow-ArchivedWorkflow-IsSuccessful 'JetFlow.ArchivedWorkflow.IsSuccessful')
  - [Name](#P-JetFlow-ArchivedWorkflow-Name 'JetFlow.ArchivedWorkflow.Name')
  - [Options](#P-JetFlow-ArchivedWorkflow-Options 'JetFlow.ArchivedWorkflow.Options')
  - [SchedulerId](#P-JetFlow-ArchivedWorkflow-SchedulerId 'JetFlow.ArchivedWorkflow.SchedulerId')
  - [StartedAt](#P-JetFlow-ArchivedWorkflow-StartedAt 'JetFlow.ArchivedWorkflow.StartedAt')
  - [Steps](#P-JetFlow-ArchivedWorkflow-Steps 'JetFlow.ArchivedWorkflow.Steps')
- [IActivity](#T-JetFlow-Interfaces-IActivity 'JetFlow.Interfaces.IActivity')
  - [ExecuteAsync(state,cancellationToken)](#M-JetFlow-Interfaces-IActivity-ExecuteAsync-JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken- 'JetFlow.Interfaces.IActivity.ExecuteAsync(JetFlow.Interfaces.IWorkflowState,System.Threading.CancellationToken)')
- [IActivityWithReturn\`1](#T-JetFlow-Interfaces-IActivityWithReturn`1 'JetFlow.Interfaces.IActivityWithReturn`1')
  - [ExecuteAsync(state,cancellationToken)](#M-JetFlow-Interfaces-IActivityWithReturn`1-ExecuteAsync-JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken- 'JetFlow.Interfaces.IActivityWithReturn`1.ExecuteAsync(JetFlow.Interfaces.IWorkflowState,System.Threading.CancellationToken)')
- [IActivityWithReturn\`2](#T-JetFlow-Interfaces-IActivityWithReturn`2 'JetFlow.Interfaces.IActivityWithReturn`2')
  - [ExecuteAsync(input,state,cancellationToken)](#M-JetFlow-Interfaces-IActivityWithReturn`2-ExecuteAsync-`1,JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken- 'JetFlow.Interfaces.IActivityWithReturn`2.ExecuteAsync(`1,JetFlow.Interfaces.IWorkflowState,System.Threading.CancellationToken)')
- [IActivity\`1](#T-JetFlow-Interfaces-IActivity`1 'JetFlow.Interfaces.IActivity`1')
  - [ExecuteAsync(input,state,cancellationToken)](#M-JetFlow-Interfaces-IActivity`1-ExecuteAsync-`0,JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken- 'JetFlow.Interfaces.IActivity`1.ExecuteAsync(`0,JetFlow.Interfaces.IWorkflowState,System.Threading.CancellationToken)')
- [IConnection](#T-JetFlow-Interfaces-IConnection 'JetFlow.Interfaces.IConnection')
  - [DelayStartWorkflowAsync\`\`1(delay,options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-DelayStartWorkflowAsync``1-System-TimeSpan,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.DelayStartWorkflowAsync``1(System.TimeSpan,JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [DelayStartWorkflowAsync\`\`2(input,delay,options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-DelayStartWorkflowAsync``2-``1,System-TimeSpan,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.DelayStartWorkflowAsync``2(``1,System.TimeSpan,JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [RegisterWorkflowActivityAsync\`\`1(activity,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityAsync``1-``0,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowActivityAsync``1(``0,System.Threading.CancellationToken)')
  - [RegisterWorkflowActivityAsync\`\`2(activity,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityAsync``2-``0,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowActivityAsync``2(``0,System.Threading.CancellationToken)')
  - [RegisterWorkflowActivityWithReturnAsync\`\`2(activity,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityWithReturnAsync``2-``0,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowActivityWithReturnAsync``2(``0,System.Threading.CancellationToken)')
  - [RegisterWorkflowActivityWithReturnAsync\`\`3(activity,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityWithReturnAsync``3-``0,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowActivityWithReturnAsync``3(``0,System.Threading.CancellationToken)')
  - [RegisterWorkflowAsync\`\`1(options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowAsync``1-JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowAsync``1(JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [RegisterWorkflowAsync\`\`2(options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-RegisterWorkflowAsync``2-JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.RegisterWorkflowAsync``2(JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [ScheduleWorkflowAsync\`\`1(schedule,options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-ScheduleWorkflowAsync``1-JetFlow-Interfaces-IWorkflowSchedule,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.ScheduleWorkflowAsync``1(JetFlow.Interfaces.IWorkflowSchedule,JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [ScheduleWorkflowAsync\`\`2(input,schedule,options,cancellationToken)](#M-JetFlow-Interfaces-IConnection-ScheduleWorkflowAsync``2-``1,JetFlow-Interfaces-IWorkflowSchedule,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.ScheduleWorkflowAsync``2(``1,JetFlow.Interfaces.IWorkflowSchedule,JetFlow.Configs.WorkflowOptions,System.Threading.CancellationToken)')
  - [StartWorkflowAsync\`\`1(cancellationToken)](#M-JetFlow-Interfaces-IConnection-StartWorkflowAsync``1-System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.StartWorkflowAsync``1(System.Threading.CancellationToken)')
  - [StartWorkflowAsync\`\`2(input,cancellationToken)](#M-JetFlow-Interfaces-IConnection-StartWorkflowAsync``2-``1,System-Threading-CancellationToken- 'JetFlow.Interfaces.IConnection.StartWorkflowAsync``2(``1,System.Threading.CancellationToken)')
- [IWorkflow](#T-JetFlow-Interfaces-IWorkflow 'JetFlow.Interfaces.IWorkflow')
  - [ExecuteAsync(context)](#M-JetFlow-Interfaces-IWorkflow-ExecuteAsync-JetFlow-Interfaces-IWorkflowContext- 'JetFlow.Interfaces.IWorkflow.ExecuteAsync(JetFlow.Interfaces.IWorkflowContext)')
- [IWorkflowContext](#T-JetFlow-Interfaces-IWorkflowContext 'JetFlow.Interfaces.IWorkflowContext')
  - [ExecuteActivityAsync\`\`1(executionRequest)](#M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``1-JetFlow-ActivityExecutionRequest- 'JetFlow.Interfaces.IWorkflowContext.ExecuteActivityAsync``1(JetFlow.ActivityExecutionRequest)')
  - [ExecuteActivityAsync\`\`2(executionRequest)](#M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``2-JetFlow-ActivityExecutionRequest{``1}- 'JetFlow.Interfaces.IWorkflowContext.ExecuteActivityAsync``2(JetFlow.ActivityExecutionRequest{``1})')
  - [ExecuteActivityAsync\`\`2(executionRequest)](#M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``2-JetFlow-ActivityExecutionRequest- 'JetFlow.Interfaces.IWorkflowContext.ExecuteActivityAsync``2(JetFlow.ActivityExecutionRequest)')
  - [ExecuteActivityAsync\`\`3(executionRequest)](#M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``3-JetFlow-ActivityExecutionRequest{``2}- 'JetFlow.Interfaces.IWorkflowContext.ExecuteActivityAsync``3(JetFlow.ActivityExecutionRequest{``2})')
  - [WaitAsync(delay)](#M-JetFlow-Interfaces-IWorkflowContext-WaitAsync-System-TimeSpan- 'JetFlow.Interfaces.IWorkflowContext.WaitAsync(System.TimeSpan)')
- [IWorkflowSchedule](#T-JetFlow-Interfaces-IWorkflowSchedule 'JetFlow.Interfaces.IWorkflowSchedule')
  - [AsString](#P-JetFlow-Interfaces-IWorkflowSchedule-AsString 'JetFlow.Interfaces.IWorkflowSchedule.AsString')
- [IWorkflowState](#T-JetFlow-Interfaces-IWorkflowState 'JetFlow.Interfaces.IWorkflowState')
  - [ActivityAttempt](#P-JetFlow-Interfaces-IWorkflowState-ActivityAttempt 'JetFlow.Interfaces.IWorkflowState.ActivityAttempt')
  - [GetActivityResultValueAsync\`\`1(activityName)](#M-JetFlow-Interfaces-IWorkflowState-GetActivityResultValueAsync``1-System-String- 'JetFlow.Interfaces.IWorkflowState.GetActivityResultValueAsync``1(System.String)')
  - [GetActivityResultValueAsync\`\`2()](#M-JetFlow-Interfaces-IWorkflowState-GetActivityResultValueAsync``2 'JetFlow.Interfaces.IWorkflowState.GetActivityResultValueAsync``2')
- [IWorkflow\`1](#T-JetFlow-Interfaces-IWorkflow`1 'JetFlow.Interfaces.IWorkflow`1')
  - [ExecuteAsync(context,input)](#M-JetFlow-Interfaces-IWorkflow`1-ExecuteAsync-JetFlow-Interfaces-IWorkflowContext,`0- 'JetFlow.Interfaces.IWorkflow`1.ExecuteAsync(JetFlow.Interfaces.IWorkflowContext,`0)')
- [RetryTypes](#T-JetFlow-RetryTypes 'JetFlow.RetryTypes')
  - [Error](#F-JetFlow-RetryTypes-Error 'JetFlow.RetryTypes.Error')
  - [Timeout](#F-JetFlow-RetryTypes-Timeout 'JetFlow.RetryTypes.Timeout')
- [WorkflowCompletionActions](#T-JetFlow-Configs-WorkflowCompletionActions 'JetFlow.Configs.WorkflowCompletionActions')
  - [ArchiveThenNothing](#F-JetFlow-Configs-WorkflowCompletionActions-ArchiveThenNothing 'JetFlow.Configs.WorkflowCompletionActions.ArchiveThenNothing')
  - [ArchiveThenPurge](#F-JetFlow-Configs-WorkflowCompletionActions-ArchiveThenPurge 'JetFlow.Configs.WorkflowCompletionActions.ArchiveThenPurge')
  - [None](#F-JetFlow-Configs-WorkflowCompletionActions-None 'JetFlow.Configs.WorkflowCompletionActions.None')
  - [Purge](#F-JetFlow-Configs-WorkflowCompletionActions-Purge 'JetFlow.Configs.WorkflowCompletionActions.Purge')
- [WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions')
  - [CompletionAction](#P-JetFlow-Configs-WorkflowOptions-CompletionAction 'JetFlow.Configs.WorkflowOptions.CompletionAction')
  - [ErrorOnActivityFailure](#P-JetFlow-Configs-WorkflowOptions-ErrorOnActivityFailure 'JetFlow.Configs.WorkflowOptions.ErrorOnActivityFailure')
  - [ErrorOnActivityTimeout](#P-JetFlow-Configs-WorkflowOptions-ErrorOnActivityTimeout 'JetFlow.Configs.WorkflowOptions.ErrorOnActivityTimeout')
  - [PurgeDelay](#P-JetFlow-Configs-WorkflowOptions-PurgeDelay 'JetFlow.Configs.WorkflowOptions.PurgeDelay')
- [WorkflowScheduleBuilder](#T-JetFlow-WorkflowScheduleBuilder 'JetFlow.WorkflowScheduleBuilder')
  - [AtDay(day)](#M-JetFlow-WorkflowScheduleBuilder-AtDay-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtDay(System.Int32)')
  - [AtDayOfWeek(dayOfWeek)](#M-JetFlow-WorkflowScheduleBuilder-AtDayOfWeek-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtDayOfWeek(System.Int32)')
  - [AtDays(days)](#M-JetFlow-WorkflowScheduleBuilder-AtDays-System-Int32[]- 'JetFlow.WorkflowScheduleBuilder.AtDays(System.Int32[])')
  - [AtDaysOfWeek(daysOfWeek)](#M-JetFlow-WorkflowScheduleBuilder-AtDaysOfWeek-System-Int32[]- 'JetFlow.WorkflowScheduleBuilder.AtDaysOfWeek(System.Int32[])')
  - [AtHour(hour)](#M-JetFlow-WorkflowScheduleBuilder-AtHour-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtHour(System.Int32)')
  - [AtHours(hours)](#M-JetFlow-WorkflowScheduleBuilder-AtHours-System-Int32[]- 'JetFlow.WorkflowScheduleBuilder.AtHours(System.Int32[])')
  - [AtMinute(minute)](#M-JetFlow-WorkflowScheduleBuilder-AtMinute-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtMinute(System.Int32)')
  - [AtMinutes(minutes)](#M-JetFlow-WorkflowScheduleBuilder-AtMinutes-System-Int32[]- 'JetFlow.WorkflowScheduleBuilder.AtMinutes(System.Int32[])')
  - [AtMonth(month)](#M-JetFlow-WorkflowScheduleBuilder-AtMonth-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtMonth(System.Int32)')
  - [AtMonths(months)](#M-JetFlow-WorkflowScheduleBuilder-AtMonths-System-Int32[]- 'JetFlow.WorkflowScheduleBuilder.AtMonths(System.Int32[])')
  - [AtSecond(second)](#M-JetFlow-WorkflowScheduleBuilder-AtSecond-System-Int32- 'JetFlow.WorkflowScheduleBuilder.AtSecond(System.Int32)')
  - [Build()](#M-JetFlow-WorkflowScheduleBuilder-Build 'JetFlow.WorkflowScheduleBuilder.Build')
  - [DailyAt(hour,minute)](#M-JetFlow-WorkflowScheduleBuilder-DailyAt-System-Int32,System-Int32- 'JetFlow.WorkflowScheduleBuilder.DailyAt(System.Int32,System.Int32)')
  - [EveryDays(days)](#M-JetFlow-WorkflowScheduleBuilder-EveryDays-System-Int32- 'JetFlow.WorkflowScheduleBuilder.EveryDays(System.Int32)')
  - [EveryHours(hours)](#M-JetFlow-WorkflowScheduleBuilder-EveryHours-System-Int32- 'JetFlow.WorkflowScheduleBuilder.EveryHours(System.Int32)')
  - [EveryMinutes(minutes)](#M-JetFlow-WorkflowScheduleBuilder-EveryMinutes-System-Int32- 'JetFlow.WorkflowScheduleBuilder.EveryMinutes(System.Int32)')
  - [EveryMonths(months)](#M-JetFlow-WorkflowScheduleBuilder-EveryMonths-System-Int32- 'JetFlow.WorkflowScheduleBuilder.EveryMonths(System.Int32)')
  - [EverySeconds(seconds)](#M-JetFlow-WorkflowScheduleBuilder-EverySeconds-System-Int32- 'JetFlow.WorkflowScheduleBuilder.EverySeconds(System.Int32)')
  - [MonthlyOn(dayOfMonth)](#M-JetFlow-WorkflowScheduleBuilder-MonthlyOn-System-Int32- 'JetFlow.WorkflowScheduleBuilder.MonthlyOn(System.Int32)')
  - [MonthlyOnAt(dayOfMonth,hour,minute)](#M-JetFlow-WorkflowScheduleBuilder-MonthlyOnAt-System-Int32,System-Int32,System-Int32- 'JetFlow.WorkflowScheduleBuilder.MonthlyOnAt(System.Int32,System.Int32,System.Int32)')
  - [Parse(cronString)](#M-JetFlow-WorkflowScheduleBuilder-Parse-System-String- 'JetFlow.WorkflowScheduleBuilder.Parse(System.String)')
  - [WeekDays()](#M-JetFlow-WorkflowScheduleBuilder-WeekDays 'JetFlow.WorkflowScheduleBuilder.WeekDays')
  - [WeekDaysAt(hour,minute)](#M-JetFlow-WorkflowScheduleBuilder-WeekDaysAt-System-Int32,System-Int32- 'JetFlow.WorkflowScheduleBuilder.WeekDaysAt(System.Int32,System.Int32)')
- [WorkflowStep](#T-JetFlow-WorkflowStep 'JetFlow.WorkflowStep')
  - [#ctor(Type,Index,Name,StartTime,EndTime,Retries,Status,ErrorMessage,Result)](#M-JetFlow-WorkflowStep-#ctor-JetFlow-WorkflowStepTypes,System-Nullable{System-UInt32},System-String,System-DateTimeOffset,System-DateTimeOffset,JetFlow-WorkflowStepRetry[],System-Nullable{JetFlow-ActivityResultStatus},System-String,System-Object- 'JetFlow.WorkflowStep.#ctor(JetFlow.WorkflowStepTypes,System.Nullable{System.UInt32},System.String,System.DateTimeOffset,System.DateTimeOffset,JetFlow.WorkflowStepRetry[],System.Nullable{JetFlow.ActivityResultStatus},System.String,System.Object)')
  - [EndTime](#P-JetFlow-WorkflowStep-EndTime 'JetFlow.WorkflowStep.EndTime')
  - [ErrorMessage](#P-JetFlow-WorkflowStep-ErrorMessage 'JetFlow.WorkflowStep.ErrorMessage')
  - [Index](#P-JetFlow-WorkflowStep-Index 'JetFlow.WorkflowStep.Index')
  - [Name](#P-JetFlow-WorkflowStep-Name 'JetFlow.WorkflowStep.Name')
  - [Result](#P-JetFlow-WorkflowStep-Result 'JetFlow.WorkflowStep.Result')
  - [Retries](#P-JetFlow-WorkflowStep-Retries 'JetFlow.WorkflowStep.Retries')
  - [StartTime](#P-JetFlow-WorkflowStep-StartTime 'JetFlow.WorkflowStep.StartTime')
  - [Status](#P-JetFlow-WorkflowStep-Status 'JetFlow.WorkflowStep.Status')
  - [Type](#P-JetFlow-WorkflowStep-Type 'JetFlow.WorkflowStep.Type')
- [WorkflowStepRetry](#T-JetFlow-WorkflowStepRetry 'JetFlow.WorkflowStepRetry')
  - [#ctor(RetryType,Timestamp)](#M-JetFlow-WorkflowStepRetry-#ctor-JetFlow-RetryTypes,System-DateTimeOffset- 'JetFlow.WorkflowStepRetry.#ctor(JetFlow.RetryTypes,System.DateTimeOffset)')
  - [RetryType](#P-JetFlow-WorkflowStepRetry-RetryType 'JetFlow.WorkflowStepRetry.RetryType')
  - [Timestamp](#P-JetFlow-WorkflowStepRetry-Timestamp 'JetFlow.WorkflowStepRetry.Timestamp')
- [WorkflowStepTypes](#T-JetFlow-WorkflowStepTypes 'JetFlow.WorkflowStepTypes')
  - [Action](#F-JetFlow-WorkflowStepTypes-Action 'JetFlow.WorkflowStepTypes.Action')
  - [Delay](#F-JetFlow-WorkflowStepTypes-Delay 'JetFlow.WorkflowStepTypes.Delay')

<a name='T-JetFlow-ActivityExecutionRequest'></a>
## ActivityExecutionRequest `type`

##### Namespace

JetFlow

##### Summary

Represents a request to execute an activity, including optional timeout and retry configurations.

##### Remarks

Use this type to specify execution parameters for an activity, such as custom timeouts or retry
behavior. Both configuration properties are optional; if not set, default execution settings will be used.

<a name='M-JetFlow-ActivityExecutionRequest-#ctor'></a>
### #ctor() `constructor`

##### Summary

Represents a request to execute an activity, including optional timeout and retry configurations.

##### Parameters

This constructor has no parameters.

##### Remarks

Use this type to specify execution parameters for an activity, such as custom timeouts or retry
behavior. Both configuration properties are optional; if not set, default execution settings will be used.

<a name='P-JetFlow-ActivityExecutionRequest-Retries'></a>
### Retries `property`

##### Summary

Gets the retry configuration to use for the activity, or null if no retries are configured.

<a name='P-JetFlow-ActivityExecutionRequest-Timeouts'></a>
### Timeouts `property`

##### Summary

Gets the timeout configuration for activity execution.

##### Remarks

Use this property to specify custom timeout settings for activities. If not set, default
timeouts may apply.

<a name='T-JetFlow-ActivityExecutionRequest`1'></a>
## ActivityExecutionRequest\`1 `type`

##### Namespace

JetFlow

##### Summary

Represents a request to execute an activity with a strongly typed input parameter.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Input | [T:JetFlow.ActivityExecutionRequest\`1](#T-T-JetFlow-ActivityExecutionRequest`1 'T:JetFlow.ActivityExecutionRequest`1') | The input value to pass to the activity. May be null if the activity does not require input. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TInput | The type of the input parameter provided to the activity. Can be any serializable type appropriate for the
activity's requirements. |

<a name='M-JetFlow-ActivityExecutionRequest`1-#ctor-`0-'></a>
### #ctor(Input) `constructor`

##### Summary

Represents a request to execute an activity with a strongly typed input parameter.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Input | [\`0](#T-`0 '`0') | The input value to pass to the activity. May be null if the activity does not require input. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TInput | The type of the input parameter provided to the activity. Can be any serializable type appropriate for the
activity's requirements. |

<a name='P-JetFlow-ActivityExecutionRequest`1-Input'></a>
### Input `property`

##### Summary

The input value to pass to the activity. May be null if the activity does not require input.

<a name='T-JetFlow-ActivityResult'></a>
## ActivityResult `type`

##### Namespace

JetFlow

##### Summary

Represents the result of an activity execution, including its status, index, and an optional error message.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Index | [T:JetFlow.ActivityResult](#T-T-JetFlow-ActivityResult 'T:JetFlow.ActivityResult') | The zero-based index of the activity within the workflow or sequence. |

<a name='M-JetFlow-ActivityResult-#ctor-System-UInt64,JetFlow-ActivityResultStatus,System-String-'></a>
### #ctor(Index,Status,ErrorMessage) `constructor`

##### Summary

Represents the result of an activity execution, including its status, index, and an optional error message.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Index | [System.UInt64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt64 'System.UInt64') | The zero-based index of the activity within the workflow or sequence. |
| Status | [JetFlow.ActivityResultStatus](#T-JetFlow-ActivityResultStatus 'JetFlow.ActivityResultStatus') | The status indicating the outcome of the activity execution. |
| ErrorMessage | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | An optional error message describing the reason for failure if the activity did not succeed; otherwise, null. |

<a name='P-JetFlow-ActivityResult-ErrorMessage'></a>
### ErrorMessage `property`

##### Summary

An optional error message describing the reason for failure if the activity did not succeed; otherwise, null.

<a name='P-JetFlow-ActivityResult-Index'></a>
### Index `property`

##### Summary

The zero-based index of the activity within the workflow or sequence.

<a name='P-JetFlow-ActivityResult-Status'></a>
### Status `property`

##### Summary

The status indicating the outcome of the activity execution.

<a name='T-JetFlow-ActivityResultStatus'></a>
## ActivityResultStatus `type`

##### Namespace

JetFlow

##### Summary

Represents the status of an activity result, indicating whether it was successful, failed, or timed out.

<a name='F-JetFlow-ActivityResultStatus-Failure'></a>
### Failure `constants`

##### Summary

Represents a failure result or state.

<a name='F-JetFlow-ActivityResultStatus-Success'></a>
### Success `constants`

##### Summary

Indicates that the operation completed successfully.

<a name='F-JetFlow-ActivityResultStatus-Timeout'></a>
### Timeout `constants`

##### Summary

Gets or sets the timeout interval for the operation.

##### Remarks

Specify the duration to wait before the operation is canceled due to timeout. The value is
typically expressed in milliseconds unless otherwise noted by the implementation.

<a name='T-JetFlow-ActivityResult`1'></a>
## ActivityResult\`1 `type`

##### Namespace

JetFlow

##### Summary

Represents the result of an activity execution, including its status, optional error message, and an output value of
the specified type.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Index | [T:JetFlow.ActivityResult\`1](#T-T-JetFlow-ActivityResult`1 'T:JetFlow.ActivityResult`1') | The zero-based index of the activity execution within a sequence or workflow. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TOutput | The type of the output value returned by the activity. May be null or default if the activity did not produce a
result. |

<a name='M-JetFlow-ActivityResult`1-#ctor-System-UInt64,JetFlow-ActivityResultStatus,System-String,`0-'></a>
### #ctor(Index,Status,ErrorMessage,Output) `constructor`

##### Summary

Represents the result of an activity execution, including its status, optional error message, and an output value of
the specified type.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Index | [System.UInt64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt64 'System.UInt64') | The zero-based index of the activity execution within a sequence or workflow. |
| Status | [JetFlow.ActivityResultStatus](#T-JetFlow-ActivityResultStatus 'JetFlow.ActivityResultStatus') | The status indicating whether the activity succeeded, failed, or was skipped. |
| ErrorMessage | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | An optional error message describing the reason for failure if the activity did not succeed; otherwise, null. |
| Output | [\`0](#T-`0 '`0') | The output value produced by the activity, or the default value for the type if no output was generated. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TOutput | The type of the output value returned by the activity. May be null or default if the activity did not produce a
result. |

<a name='P-JetFlow-ActivityResult`1-Output'></a>
### Output `property`

##### Summary

The output value produced by the activity, or the default value for the type if no output was generated.

<a name='T-JetFlow-ActivityRetryConfiguration'></a>
## ActivityRetryConfiguration `type`

##### Namespace

JetFlow

##### Summary

Houses the Retry configuration for an activity execution. This includes the maximum number of attempts, the delay between attempts, and which errors to retry on or block on.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| MaximumAttempts | [T:JetFlow.ActivityRetryConfiguration](#T-T-JetFlow-ActivityRetryConfiguration 'T:JetFlow.ActivityRetryConfiguration') | The maximum number of retry attempts. |

<a name='M-JetFlow-ActivityRetryConfiguration-#ctor-System-UInt16,System-Nullable{System-TimeSpan},System-Boolean,System-Boolean,System-String[]-'></a>
### #ctor(MaximumAttempts,DelayBetween,RetryOnTimeout,RetryOnError,BlockedErrors) `constructor`

##### Summary

Houses the Retry configuration for an activity execution. This includes the maximum number of attempts, the delay between attempts, and which errors to retry on or block on.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| MaximumAttempts | [System.UInt16](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt16 'System.UInt16') | The maximum number of retry attempts. |
| DelayBetween | [System.Nullable{System.TimeSpan}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{System.TimeSpan}') | The delay between retry attempts. |
| RetryOnTimeout | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Indicates whether to retry on timeout. |
| RetryOnError | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Indicates whether to retry on error. |
| BlockedErrors | [System.String[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String[] 'System.String[]') | A list of errors that should block retries. |

<a name='P-JetFlow-ActivityRetryConfiguration-BlockedErrors'></a>
### BlockedErrors `property`

##### Summary

A list of errors that should block retries.

<a name='P-JetFlow-ActivityRetryConfiguration-DelayBetween'></a>
### DelayBetween `property`

##### Summary

The delay between retry attempts.

<a name='P-JetFlow-ActivityRetryConfiguration-MaximumAttempts'></a>
### MaximumAttempts `property`

##### Summary

The maximum number of retry attempts.

<a name='P-JetFlow-ActivityRetryConfiguration-RetryOnError'></a>
### RetryOnError `property`

##### Summary

Indicates whether to retry on error.

<a name='P-JetFlow-ActivityRetryConfiguration-RetryOnTimeout'></a>
### RetryOnTimeout `property`

##### Summary

Indicates whether to retry on timeout.

<a name='T-JetFlow-ActivityTimeoutConfiguration'></a>
## ActivityTimeoutConfiguration `type`

##### Namespace

JetFlow

##### Summary

Represents the timeout configuration for an activity, including overall and per-attempt timeouts.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| OverallTimeout | [T:JetFlow.ActivityTimeoutConfiguration](#T-T-JetFlow-ActivityTimeoutConfiguration 'T:JetFlow.ActivityTimeoutConfiguration') | The maximum duration allowed for the entire activity to complete. Specify `null` to indicate no
overall timeout. |

<a name='M-JetFlow-ActivityTimeoutConfiguration-#ctor-System-Nullable{System-TimeSpan},System-Nullable{System-TimeSpan}-'></a>
### #ctor(OverallTimeout,AttemptTimeout) `constructor`

##### Summary

Represents the timeout configuration for an activity, including overall and per-attempt timeouts.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| OverallTimeout | [System.Nullable{System.TimeSpan}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{System.TimeSpan}') | The maximum duration allowed for the entire activity to complete. Specify `null` to indicate no
overall timeout. |
| AttemptTimeout | [System.Nullable{System.TimeSpan}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{System.TimeSpan}') | The maximum duration allowed for a single attempt of the activity. Specify `null` to indicate no
per-attempt timeout. |

<a name='P-JetFlow-ActivityTimeoutConfiguration-AttemptTimeout'></a>
### AttemptTimeout `property`

##### Summary

The maximum duration allowed for a single attempt of the activity. Specify `null` to indicate no
per-attempt timeout.

<a name='P-JetFlow-ActivityTimeoutConfiguration-OverallTimeout'></a>
### OverallTimeout `property`

##### Summary

The maximum duration allowed for the entire activity to complete. Specify `null` to indicate no
overall timeout.

<a name='T-JetFlow-ArchivedWorkflow'></a>
## ArchivedWorkflow `type`

##### Namespace

JetFlow

##### Summary

Represents a workflow instance that has completed execution and has been archived, including its metadata, execution
details, and results.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ID | [T:JetFlow.ArchivedWorkflow](#T-T-JetFlow-ArchivedWorkflow 'T:JetFlow.ArchivedWorkflow') | The unique identifier of the archived workflow instance. |

<a name='M-JetFlow-ArchivedWorkflow-#ctor-System-Guid,System-Nullable{System-Guid},System-String,JetFlow-Configs-WorkflowOptions,System-DateTimeOffset,System-DateTimeOffset,System-Boolean,System-String,System-Object,JetFlow-WorkflowStep[]-'></a>
### #ctor(ID,SchedulerId,Name,Options,StartedAt,FinishedAt,IsSuccessful,ErrorMessage,Arguments,Steps) `constructor`

##### Summary

Represents a workflow instance that has completed execution and has been archived, including its metadata, execution
details, and results.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ID | [System.Guid](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Guid 'System.Guid') | The unique identifier of the archived workflow instance. |
| SchedulerId | [System.Nullable{System.Guid}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{System.Guid}') | The identifier of the scheduler that executed the workflow, or null if not applicable. |
| Name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the workflow definition associated with this instance. |
| Options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | The options used to configure the workflow execution. |
| StartedAt | [System.DateTimeOffset](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTimeOffset 'System.DateTimeOffset') | The date and time, in UTC, when the workflow execution started. |
| FinishedAt | [System.DateTimeOffset](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTimeOffset 'System.DateTimeOffset') | The date and time, in UTC, when the workflow execution finished. |
| IsSuccessful | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | true if the workflow completed successfully; otherwise, false. |
| ErrorMessage | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The error message if the workflow failed; otherwise, null. |
| Arguments | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') | The arguments provided to the workflow at the time of execution, or null if none. |
| Steps | [JetFlow.WorkflowStep[]](#T-JetFlow-WorkflowStep[] 'JetFlow.WorkflowStep[]') | An array containing the steps executed as part of the workflow, in order. |

<a name='P-JetFlow-ArchivedWorkflow-Arguments'></a>
### Arguments `property`

##### Summary

The arguments provided to the workflow at the time of execution, or null if none.

<a name='P-JetFlow-ArchivedWorkflow-ErrorMessage'></a>
### ErrorMessage `property`

##### Summary

The error message if the workflow failed; otherwise, null.

<a name='P-JetFlow-ArchivedWorkflow-FinishedAt'></a>
### FinishedAt `property`

##### Summary

The date and time, in UTC, when the workflow execution finished.

<a name='P-JetFlow-ArchivedWorkflow-ID'></a>
### ID `property`

##### Summary

The unique identifier of the archived workflow instance.

<a name='P-JetFlow-ArchivedWorkflow-IsSuccessful'></a>
### IsSuccessful `property`

##### Summary

true if the workflow completed successfully; otherwise, false.

<a name='P-JetFlow-ArchivedWorkflow-Name'></a>
### Name `property`

##### Summary

The name of the workflow definition associated with this instance.

<a name='P-JetFlow-ArchivedWorkflow-Options'></a>
### Options `property`

##### Summary

The options used to configure the workflow execution.

<a name='P-JetFlow-ArchivedWorkflow-SchedulerId'></a>
### SchedulerId `property`

##### Summary

The identifier of the scheduler that executed the workflow, or null if not applicable.

<a name='P-JetFlow-ArchivedWorkflow-StartedAt'></a>
### StartedAt `property`

##### Summary

The date and time, in UTC, when the workflow execution started.

<a name='P-JetFlow-ArchivedWorkflow-Steps'></a>
### Steps `property`

##### Summary

An array containing the steps executed as part of the workflow, in order.

<a name='T-JetFlow-Interfaces-IActivity'></a>
## IActivity `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents an activity that can be executed as part of a workflow. Activities are the building blocks of workflows and can perform various tasks, such as data processing, API calls, or any custom logic defined by the user. The IActivity interface defines a contract for executing an activity asynchronously, allowing for flexible and scalable workflow implementations.

<a name='M-JetFlow-Interfaces-IActivity-ExecuteAsync-JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken-'></a>
### ExecuteAsync(state,cancellationToken) `method`

##### Summary

Executes the workflow operation asynchronously using the specified workflow state.

##### Returns

A task that represents the asynchronous execution operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [JetFlow.Interfaces.IWorkflowState](#T-JetFlow-Interfaces-IWorkflowState 'JetFlow.Interfaces.IWorkflowState') | The workflow state to use during execution. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the asynchronous operation. |

<a name='T-JetFlow-Interfaces-IActivityWithReturn`1'></a>
## IActivityWithReturn\`1 `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents an activity that can be executed as part of a workflow, with a specific output type. This interface extends the IActivity interface and allows for activities that produce a result upon execution. The generic type parameter TOutput specifies the type of output that the activity returns, enabling type safety and flexibility in workflow design. Activities implementing this interface can be used to perform operations that yield a result, which can then be utilized in subsequent steps of the workflow.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TOutput | The type of output that the activity returns. |

<a name='M-JetFlow-Interfaces-IActivityWithReturn`1-ExecuteAsync-JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken-'></a>
### ExecuteAsync(state,cancellationToken) `method`

##### Summary

Executes the workflow operation asynchronously using the specified state and cancellation token.

##### Returns

A task that represents the asynchronous operation. The task result contains the output produced by the workflow.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [JetFlow.Interfaces.IWorkflowState](#T-JetFlow-Interfaces-IWorkflowState 'JetFlow.Interfaces.IWorkflowState') | The workflow state to use during execution. Provides context and data required by the workflow. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token that can be used to cancel the asynchronous operation. |

<a name='T-JetFlow-Interfaces-IActivityWithReturn`2'></a>
## IActivityWithReturn\`2 `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents an activity that can be executed as part of a workflow, with specific input and output types. This interface extends the IActivity interface and allows for activities that require input data and produce a result upon execution. The generic type parameters TOutput and TInput specify the types of output and input, respectively, enabling type safety and flexibility in workflow design. Activities implementing this interface can be used to perform operations that take input data, process it, and yield a result, which can then be utilized in subsequent steps of the workflow.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TOutput | The type of output that the activity returns. |
| TInput | The type of input that the activity expects. |

<a name='M-JetFlow-Interfaces-IActivityWithReturn`2-ExecuteAsync-`1,JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken-'></a>
### ExecuteAsync(input,state,cancellationToken) `method`

##### Summary

Executes the workflow operation asynchronously using the specified input and workflow state.

##### Returns

A task that represents the asynchronous operation. The task result contains the output produced by the workflow.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| input | [\`1](#T-`1 '`1') | The input data for the workflow execution. May be null if the workflow does not require input. |
| state | [JetFlow.Interfaces.IWorkflowState](#T-JetFlow-Interfaces-IWorkflowState 'JetFlow.Interfaces.IWorkflowState') | The current workflow state used to track execution progress and context. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token that can be used to cancel the asynchronous operation. |

<a name='T-JetFlow-Interfaces-IActivity`1'></a>
## IActivity\`1 `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents an activity that can be executed as part of a workflow, with a specific input type. This interface extends the IActivity interface and allows for activities that require input data to be executed within a workflow. The generic type parameter TInput specifies the type of input that the activity expects, enabling type safety and flexibility in workflow design.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TInput | The type of input that the activity expects. |

<a name='M-JetFlow-Interfaces-IActivity`1-ExecuteAsync-`0,JetFlow-Interfaces-IWorkflowState,System-Threading-CancellationToken-'></a>
### ExecuteAsync(input,state,cancellationToken) `method`

##### Summary

Executes the workflow operation asynchronously using the specified input and workflow state.

##### Returns

A task that represents the asynchronous execution operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| input | [\`0](#T-`0 '`0') | The input data for the activity. Can be null if the activity does not require input. |
| state | [JetFlow.Interfaces.IWorkflowState](#T-JetFlow-Interfaces-IWorkflowState 'JetFlow.Interfaces.IWorkflowState') | The workflow state to use during execution. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the asynchronous operation. |

<a name='T-JetFlow-Interfaces-IConnection'></a>
## IConnection `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents a connection to the workflow engine, allowing you to register workflows and activities, start workflows, and schedule workflows.

<a name='M-JetFlow-Interfaces-IConnection-DelayStartWorkflowAsync``1-System-TimeSpan,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### DelayStartWorkflowAsync\`\`1(delay,options,cancellationToken) `method`

##### Summary

Schedules the specified workflow to start after the given delay.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
scheduled workflow instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| delay | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | The amount of time to wait before starting the workflow. Must be a non-negative duration. |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | Optional settings that configure the workflow execution. If null, default options are used. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token that can be used to cancel the scheduling operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to start. Must implement the IWorkflow interface. |

<a name='M-JetFlow-Interfaces-IConnection-DelayStartWorkflowAsync``2-``1,System-TimeSpan,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### DelayStartWorkflowAsync\`\`2(input,delay,options,cancellationToken) `method`

##### Summary

Schedules the specified workflow to start after the given delay with the provided input.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
scheduled workflow instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| input | [\`\`1](#T-``1 '``1') | The input data to pass to the workflow when it starts. |
| delay | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | The amount of time to wait before starting the workflow. |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | Optional configuration options for the workflow execution. If null, default options are used. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token that can be used to cancel the scheduling operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to start. Must implement IWorkflow<TInput>. |
| TInput | The type of input required by the workflow. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityAsync``1-``0,System-Threading-CancellationToken-'></a>
### RegisterWorkflowActivityAsync\`\`1(activity,cancellationToken) `method`

##### Summary

Registers the specified workflow activity for execution within the workflow host.

##### Returns

A ValueTask that represents the asynchronous registration operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| activity | [\`\`0](#T-``0 '``0') | The workflow activity instance to register. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the registration operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflowActivity | The type of the workflow activity to register. Must implement the IActivity interface. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityAsync``2-``0,System-Threading-CancellationToken-'></a>
### RegisterWorkflowActivityAsync\`\`2(activity,cancellationToken) `method`

##### Summary

Registers a workflow activity for execution within the workflow host.

##### Returns

A ValueTask that represents the asynchronous registration operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| activity | [\`\`0](#T-``0 '``0') | The workflow activity instance to register. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the registration operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflowActivity | The type of the workflow activity to register. Must implement IActivity<TInput>. |
| TInput | The type of input accepted by the workflow activity. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityWithReturnAsync``2-``0,System-Threading-CancellationToken-'></a>
### RegisterWorkflowActivityWithReturnAsync\`\`2(activity,cancellationToken) `method`

##### Summary

Registers a workflow activity that produces a return value for execution within the workflow runtime.

##### Returns

A ValueTask that represents the asynchronous registration operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| activity | [\`\`0](#T-``0 '``0') | The workflow activity instance to register. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the registration operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflowActivity | The type of the workflow activity to register. Must implement IActivityWithReturn<TOutput>. |
| TOutput | The type of the value returned by the workflow activity. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowActivityWithReturnAsync``3-``0,System-Threading-CancellationToken-'></a>
### RegisterWorkflowActivityWithReturnAsync\`\`3(activity,cancellationToken) `method`

##### Summary

Registers a workflow activity that produces a return value and accepts input for execution within the workflow runtime.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| activity | [\`\`0](#T-``0 '``0') | The workflow activity instance to register. Cannot be null. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflowActivity | The type of the workflow activity to register. Must implement IActivityWithReturn<TOutput, TInput>. |
| TOutput | The type of the value returned by the workflow activity. |
| TInput | The type of input accepted by the workflow activity. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowAsync``1-JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### RegisterWorkflowAsync\`\`1(options,cancellationToken) `method`

##### Summary

Registers a workflow of the specified type for execution with the provided options.

##### Returns

A ValueTask that represents the asynchronous registration operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | The options to configure the workflow registration. If null, default options are used. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token to monitor for cancellation requests. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to register. Must implement the IWorkflow interface. |

<a name='M-JetFlow-Interfaces-IConnection-RegisterWorkflowAsync``2-JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### RegisterWorkflowAsync\`\`2(options,cancellationToken) `method`

##### Summary

Registers a workflow type for execution with the specified options.

##### Returns

A ValueTask that represents the asynchronous registration operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | The options to use when registering the workflow, or null to use default options. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token to monitor for cancellation requests. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The workflow type to register. Must implement IWorkflow<TInput>. |
| TInput | The type of input accepted by the workflow. |

<a name='M-JetFlow-Interfaces-IConnection-ScheduleWorkflowAsync``1-JetFlow-Interfaces-IWorkflowSchedule,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### ScheduleWorkflowAsync\`\`1(schedule,options,cancellationToken) `method`

##### Summary

Schedules a workflow of the specified type for execution according to the provided schedule.

##### Returns

A ValueTask that represents the asynchronous scheduling operation. The result contains the unique identifier
of the scheduled workflow instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| schedule | [JetFlow.Interfaces.IWorkflowSchedule](#T-JetFlow-Interfaces-IWorkflowSchedule 'JetFlow.Interfaces.IWorkflowSchedule') | The schedule that defines when and how the workflow should be executed. |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | Optional configuration options for the workflow execution. If null, default options are used. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token to monitor for cancellation requests. The operation is canceled if the token is triggered. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to schedule. Must implement the IWorkflow interface. |

<a name='M-JetFlow-Interfaces-IConnection-ScheduleWorkflowAsync``2-``1,JetFlow-Interfaces-IWorkflowSchedule,JetFlow-Configs-WorkflowOptions,System-Threading-CancellationToken-'></a>
### ScheduleWorkflowAsync\`\`2(input,schedule,options,cancellationToken) `method`

##### Summary

Schedules a workflow of the specified type to run according to the provided schedule.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
scheduled workflow.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| input | [\`\`1](#T-``1 '``1') | The input data to pass to the workflow when it is executed. |
| schedule | [JetFlow.Interfaces.IWorkflowSchedule](#T-JetFlow-Interfaces-IWorkflowSchedule 'JetFlow.Interfaces.IWorkflowSchedule') | The schedule that determines when the workflow will be executed. |
| options | [JetFlow.Configs.WorkflowOptions](#T-JetFlow-Configs-WorkflowOptions 'JetFlow.Configs.WorkflowOptions') | Optional configuration options for the workflow execution. If not specified, default options are used. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A token that can be used to cancel the scheduling operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to schedule. Must implement IWorkflow<TInput>. |
| TInput | The type of input data provided to the workflow. |

<a name='M-JetFlow-Interfaces-IConnection-StartWorkflowAsync``1-System-Threading-CancellationToken-'></a>
### StartWorkflowAsync\`\`1(cancellationToken) `method`

##### Summary

Starts a new instance of the specified workflow type asynchronously.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the unique identifier of the
started workflow instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the workflow start operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to start. Must implement the IWorkflow interface. |

<a name='M-JetFlow-Interfaces-IConnection-StartWorkflowAsync``2-``1,System-Threading-CancellationToken-'></a>
### StartWorkflowAsync\`\`2(input,cancellationToken) `method`

##### Summary

Starts a new instance of the specified workflow asynchronously.

##### Returns

A ValueTask that represents the asynchronous operation. The task result contains the unique identifier of
the started workflow instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| input | [\`\`1](#T-``1 '``1') | The input data to pass to the workflow instance. |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | A cancellation token that can be used to cancel the asynchronous operation. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflow | The type of workflow to start. Must implement IWorkflow<TInput>. |
| TInput | The type of input data provided to the workflow. |

<a name='T-JetFlow-Interfaces-IWorkflow'></a>
## IWorkflow `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents a workflow that can be executed with a given context. This interface defines the contract for executing a workflow without any input parameters.

<a name='M-JetFlow-Interfaces-IWorkflow-ExecuteAsync-JetFlow-Interfaces-IWorkflowContext-'></a>
### ExecuteAsync(context) `method`

##### Summary

Executes the workflow operations asynchronously using the specified workflow context.

##### Returns

A ValueTask that represents the asynchronous execution operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [JetFlow.Interfaces.IWorkflowContext](#T-JetFlow-Interfaces-IWorkflowContext 'JetFlow.Interfaces.IWorkflowContext') | The workflow context that provides data and services required for execution. Cannot be null. |

<a name='T-JetFlow-Interfaces-IWorkflowContext'></a>
## IWorkflowContext `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Used to execute activities and wait for a specified amount of time within a workflow. This interface is typically passed as a parameter to the workflow's main method, allowing the workflow to interact with the execution environment and manage its activities effectively.

<a name='M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``1-JetFlow-ActivityExecutionRequest-'></a>
### ExecuteActivityAsync\`\`1(executionRequest) `method`

##### Summary

Executes the specified activity asynchronously using the provided execution request and returns the result of
the activity.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
activity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| executionRequest | [JetFlow.ActivityExecutionRequest](#T-JetFlow-ActivityExecutionRequest 'JetFlow.ActivityExecutionRequest') | The request containing all information required to execute the activity. Cannot be null. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TActivity | The type of activity to execute. Must implement the IActivity interface. |

<a name='M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``2-JetFlow-ActivityExecutionRequest{``1}-'></a>
### ExecuteActivityAsync\`\`2(executionRequest) `method`

##### Summary

Executes the specified activity asynchronously using the provided execution request.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
execution.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| executionRequest | [JetFlow.ActivityExecutionRequest{\`\`1}](#T-JetFlow-ActivityExecutionRequest{``1} 'JetFlow.ActivityExecutionRequest{``1}') | The request containing the input and context information for the activity execution. Cannot be null. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TActivity | The type of activity to execute. Must implement IActivity<TInput>. |
| TInput | The type of input required by the activity. |

<a name='M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``2-JetFlow-ActivityExecutionRequest-'></a>
### ExecuteActivityAsync\`\`2(executionRequest) `method`

##### Summary

Executes the specified activity asynchronously using the provided execution request and returns the result of
the activity.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the outcome of the executed
activity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| executionRequest | [JetFlow.ActivityExecutionRequest](#T-JetFlow-ActivityExecutionRequest 'JetFlow.ActivityExecutionRequest') | The request containing all information required to execute the activity. Cannot be null. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TActivity | The type of activity to execute. Must implement IActivityWithReturn<TOutput>. |
| TOutput | The type of the value returned by the activity. |

<a name='M-JetFlow-Interfaces-IWorkflowContext-ExecuteActivityAsync``3-JetFlow-ActivityExecutionRequest{``2}-'></a>
### ExecuteActivityAsync\`\`3(executionRequest) `method`

##### Summary

Executes the specified activity asynchronously using the provided execution request and returns the result of
the activity

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the outcome of the activity
execution.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| executionRequest | [JetFlow.ActivityExecutionRequest{\`\`2}](#T-JetFlow-ActivityExecutionRequest{``2} 'JetFlow.ActivityExecutionRequest{``2}') | The request containing the input and context information for the activity execution. Cannot be null. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TActivity | The type of activity to execute. Must implement IActivityWithReturn<TOutput, TInput>. |
| TOutput | The type of the value returned by the activity. |
| TInput | The type of input required by the activity. |

<a name='M-JetFlow-Interfaces-IWorkflowContext-WaitAsync-System-TimeSpan-'></a>
### WaitAsync(delay) `method`

##### Summary

Asynchronously waits for the specified time interval before completing.

##### Returns

A ValueTask that represents the asynchronous wait operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| delay | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | The amount of time to wait before the operation completes. Must be a non-negative time span. |

<a name='T-JetFlow-Interfaces-IWorkflowSchedule'></a>
## IWorkflowSchedule `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents a workflow schedule, which can be used to define when a workflow should be executed. The schedule can be defined using a cron expression or any other string representation that the implementation supports.

<a name='P-JetFlow-Interfaces-IWorkflowSchedule-AsString'></a>
### AsString `property`

##### Summary

The string representation of the workflow schedule. This could be a cron expression or any other format that the implementation supports. The exact format and interpretation of this string will depend on the specific implementation of the workflow scheduling system.

<a name='T-JetFlow-Interfaces-IWorkflowState'></a>
## IWorkflowState `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Houses the state of a workflow, such as the current activity attempt and the results of completed activities. This interface allows workflow activities to access information about the workflow's execution state, enabling them to make informed decisions based on previous activity outcomes.

<a name='P-JetFlow-Interfaces-IWorkflowState-ActivityAttempt'></a>
### ActivityAttempt `property`

##### Summary

The current attempt number for the activity being executed. This value is incremented each time an activity is retried, allowing activities to determine how many times they have been attempted and to implement retry logic accordingly.

<a name='M-JetFlow-Interfaces-IWorkflowState-GetActivityResultValueAsync``1-System-String-'></a>
### GetActivityResultValueAsync\`\`1(activityName) `method`

##### Summary

Called by workflow activities to retrieve the result value of a previously completed activity by specifying the activity's name. This method is also generic, allowing the caller to specify the expected type of the result value. Similar to the previous method, it returns a nullable value, indicating that the result may not be present if the activity has not been completed or if it did not produce a result. This provides flexibility for activities to access results based on activity names rather than types, which can be useful in scenarios where multiple activities of the same type are executed within a workflow.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the value of the completed activity, or null if the activity has not been completed or did not produce a result.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| activityName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the activity whose result is being retrieved. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TValue | The type of the value returned by the activity. |

<a name='M-JetFlow-Interfaces-IWorkflowState-GetActivityResultValueAsync``2'></a>
### GetActivityResultValueAsync\`\`2() `method`

##### Summary

Called by workflow activities to retrieve the result value of a previously completed activity. The method is generic, allowing the caller to specify the expected type of the result value. The method returns a nullable value, indicating that the result may not be present if the activity has not been completed or if it did not produce a result. This allows activities to safely access previous results without risking exceptions due to missing data.

##### Returns

A ValueTask that represents the asynchronous operation. The result contains the value of the completed activity, or null if the activity has not been completed or did not produce a result.

##### Parameters

This method has no parameters.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TWorkflowActivity | The type of the workflow activity whose result is being retrieved. |
| TValue | The type of the value returned by the activity. |

<a name='T-JetFlow-Interfaces-IWorkflow`1'></a>
## IWorkflow\`1 `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents a workflow that can be executed with a given context and an input parameter. This interface defines the contract for executing a workflow that requires input data.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TInput |  |

<a name='M-JetFlow-Interfaces-IWorkflow`1-ExecuteAsync-JetFlow-Interfaces-IWorkflowContext,`0-'></a>
### ExecuteAsync(context,input) `method`

##### Summary

Executes the workflow operation asynchronously using the specified context and input.

##### Returns

A ValueTask that represents the asynchronous execution of the workflow operation.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [JetFlow.Interfaces.IWorkflowContext](#T-JetFlow-Interfaces-IWorkflowContext 'JetFlow.Interfaces.IWorkflowContext') | The workflow context that provides execution state and services for the operation. Cannot be null. |
| input | [\`0](#T-`0 '`0') | The input data for the workflow operation. May be null if the operation does not require input. |

<a name='T-JetFlow-RetryTypes'></a>
## RetryTypes `type`

##### Namespace

JetFlow

##### Summary

Specifies the type of retry that occurred during a workflow step, which can be either a timeout or an error.

<a name='F-JetFlow-RetryTypes-Error'></a>
### Error `constants`

##### Summary

Specifies that the retry was triggered due to an error, indicating that an exception or failure occurred during the execution of the operation.

<a name='F-JetFlow-RetryTypes-Timeout'></a>
### Timeout `constants`

##### Summary

Specifies that the retry was triggered due to a timeout, indicating that the operation took longer than expected to complete.

<a name='T-JetFlow-Configs-WorkflowCompletionActions'></a>
## WorkflowCompletionActions `type`

##### Namespace

JetFlow.Configs

##### Summary

Specifies the actions to take when a workflow completes, such as archiving or purging the workflow data. This allows for automated cleanup and management of workflow records based on the desired retention policies.

<a name='F-JetFlow-Configs-WorkflowCompletionActions-ArchiveThenNothing'></a>
### ArchiveThenNothing `constants`

##### Summary

Specifies that the item should be archived and no further action should be taken.

<a name='F-JetFlow-Configs-WorkflowCompletionActions-ArchiveThenPurge'></a>
### ArchiveThenPurge `constants`

##### Summary

Specifies that items should be archived before being purged.

##### Remarks

Use this option when it is necessary to retain a backup of items prior to permanent deletion.
Archiving ensures that data can be restored if needed after the purge operation.

<a name='F-JetFlow-Configs-WorkflowCompletionActions-None'></a>
### None `constants`

##### Summary

Default behavior where no automatic action is taken upon workflow completion. The workflow data will remain in the system until manually archived or purged. This option allows for maximum flexibility in managing workflow records, but may require manual intervention to clean up old or completed workflows.

<a name='F-JetFlow-Configs-WorkflowCompletionActions-Purge'></a>
### Purge `constants`

##### Summary

Removes all items or data from the collection or resource, resetting it to an empty state.

##### Remarks

Use this method to clear all contents. After calling this method, the collection or resource
will contain no items. Any references to previously stored items will be released if applicable.

<a name='T-JetFlow-Configs-WorkflowOptions'></a>
## WorkflowOptions `type`

##### Namespace

JetFlow.Configs

##### Summary

Represents configuration options for workflow execution, including completion actions, purge behavior, and error
handling settings.

##### Remarks

Use this type to specify how a workflow should behave upon completion, how long to retain workflow
data, and whether to treat activity timeouts or failures as errors. All properties are immutable and must be set at
initialization.

<a name='P-JetFlow-Configs-WorkflowOptions-CompletionAction'></a>
### CompletionAction `property`

##### Summary

Gets the action to perform when the workflow completes.

<a name='P-JetFlow-Configs-WorkflowOptions-ErrorOnActivityFailure'></a>
### ErrorOnActivityFailure `property`

##### Summary

Gets a value indicating whether an error should be thrown when an activity fails.

<a name='P-JetFlow-Configs-WorkflowOptions-ErrorOnActivityTimeout'></a>
### ErrorOnActivityTimeout `property`

##### Summary

Gets a value indicating whether an exception is thrown when an activity times out.

##### Remarks

When set to `true`, the operation will throw an exception if an activity
exceeds its allowed timeout period. When set to `false`, the operation may complete without
throwing, even if a timeout occurs.

<a name='P-JetFlow-Configs-WorkflowOptions-PurgeDelay'></a>
### PurgeDelay `property`

##### Summary

Gets the optional delay before purging items.

##### Remarks

If set, items will not be purged until the specified delay has elapsed. If null, items may be
purged immediately according to the default policy.

<a name='T-JetFlow-WorkflowScheduleBuilder'></a>
## WorkflowScheduleBuilder `type`

##### Namespace

JetFlow

##### Summary

Used to build a workflow schedule, which is used to specify when a workflow should be executed. The schedule is based on the cron format, which is a string format that specifies the schedule using a combination of seconds, minutes, hours, day of month, month, and day of week. This builder provides a fluent API for building a workflow schedule, and also provides a method for parsing a cron string into a workflow schedule builder.

<a name='M-JetFlow-WorkflowScheduleBuilder-AtDay-System-Int32-'></a>
### AtDay(day) `method`

##### Summary

Schedules the workflow to run at a specific day of the month. For example, calling AtDay(15) would schedule the workflow to run on the 15th day of every month. The day value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDay(15).EveryMonths(1) would schedule the workflow to run on the 15th day of every month.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| day | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific day of the month on which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtDayOfWeek-System-Int32-'></a>
### AtDayOfWeek(dayOfWeek) `method`

##### Summary

Schedules the workflow to run at a specific day of the week. For example, calling AtDayOfWeek(1) would schedule the workflow to run on Mondays every week. The dayOfWeek value must be between 1 and 7, where 1 represents Monday and 7 represents Sunday. If an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDayOfWeek(1).EveryHours(1) would schedule the workflow to run on Mondays every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| dayOfWeek | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific day of the week on which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtDays-System-Int32[]-'></a>
### AtDays(days) `method`

##### Summary

Schedules the workflow to run at specific days of the month. For example, calling AtDays(new[] { 1, 15 }) would schedule the workflow to run on the 1st and 15th day of every month. The day values must be between 1 and 31, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDays(new[] { 1, 15 }).EveryMonths(1) would schedule the workflow to run on the 1st and 15th day of every month.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| days | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | An array of specific days of the month on which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtDaysOfWeek-System-Int32[]-'></a>
### AtDaysOfWeek(daysOfWeek) `method`

##### Summary

Schedules the workflow to run at specific days of the week. For example, calling AtDaysOfWeek(new[] { 1, 5 }) would schedule the workflow to run on Mondays and Fridays every week. The dayOfWeek values must be between 1 and 7, where 1 represents Monday and 7 represents Sunday. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDaysOfWeek(new[] { 1, 5 }).EveryHours(1) would schedule the workflow to run on Mondays and Fridays every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| daysOfWeek | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | An array of specific days of the week on which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtHour-System-Int32-'></a>
### AtHour(hour) `method`

##### Summary

Schedules the workflow to run at a specific hour. For example, calling AtHour(14) would schedule the workflow to run at 2 PM every day. The hour value must be between 0 and 23, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtHour(14).EveryDays(1) would schedule the workflow to run at 2 PM every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hour | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific hour at which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtHours-System-Int32[]-'></a>
### AtHours(hours) `method`

##### Summary

Schedules the workflow to run at specific hours. For example, calling AtHours(new[] { 9, 17 }) would schedule the workflow to run at 9 AM and 5 PM every day. The hour values must be between 0 and 23, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtHours(new[] { 9, 17 }).EveryDays(1) would schedule the workflow to run at 9 AM and 5 PM every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hours | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | An array of specific hours at which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtMinute-System-Int32-'></a>
### AtMinute(minute) `method`

##### Summary

Schedules the workflow to run at a specific minute. For example, calling AtMinute(30) would schedule the workflow to run at the 30th minute of every hour. The minute value must be between 0 and 59, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMinute(30).EveryHours(1) would schedule the workflow to run at the 30th minute of every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| minute | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific minute at which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtMinutes-System-Int32[]-'></a>
### AtMinutes(minutes) `method`

##### Summary

Schedules the workflow to run at specific minutes. For example, calling AtMinutes(new[] { 15, 45 }) would schedule the workflow to run at the 15th and 45th minute of every hour. The minute values must be between 0 and 59, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMinutes(new[] { 15, 45 }).EveryHours(1) would schedule the workflow to run at the 15th and 45th minute of every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| minutes | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | An array of specific minutes at which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtMonth-System-Int32-'></a>
### AtMonth(month) `method`

##### Summary

Schedules the workflow to run at a specific month. For example, calling AtMonth(5) would schedule the workflow to run in May every year. The month value must be between 1 and 12, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMonth(5).EveryDays(1) would schedule the workflow to run in May every year and every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| month | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific month in which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtMonths-System-Int32[]-'></a>
### AtMonths(months) `method`

##### Summary

Schedules the workflow to run at specific months. For example, calling AtMonths(new[] { 3, 9 }) would schedule the workflow to run in March and September every year. The month values must be between 1 and 12, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMonths(new[] { 3, 9 }).EveryDays(1) would schedule the workflow to run in March and September every year and every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| months | [System.Int32[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32[] 'System.Int32[]') | An array of specific months in which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-AtSecond-System-Int32-'></a>
### AtSecond(second) `method`

##### Summary

Schedules the workflow to run at a specific second. For example, calling AtSecond(30) would schedule the workflow to run at the 30th second of every minute. The second value must be between 0 and 59, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtSecond(30).EveryMinutes(1) would schedule the workflow to run at the 30th second of every minute.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| second | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The specific second at which the workflow should run. |

<a name='M-JetFlow-WorkflowScheduleBuilder-Build'></a>
### Build() `method`

##### Summary

Builds the workflow schedule based on the values specified in the builder. The resulting IWorkflowSchedule will have a string representation in the cron format, which can be used to specify when a workflow should be executed. For example, if the builder is configured to run every hour at the top of the hour, the resulting IWorkflowSchedule will have an AsString value of "0 0 * * * *". If the builder is configured to run every 5 seconds, the resulting IWorkflowSchedule will have an AsString value of "*/5 * * * * *".

##### Returns

A workflow schedule that can be used to specify when a workflow should be executed.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-WorkflowScheduleBuilder-DailyAt-System-Int32,System-Int32-'></a>
### DailyAt(hour,minute) `method`

##### Summary

Schedules the workflow to run daily at a specific hour and minute. For example, calling DailyAt(14, 30) would schedule the workflow to run every day at 2:30 PM. The hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling DailyAt(14, 30).EveryMonths(1) would schedule the workflow to run every day at 2:30 PM every month.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hour | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The hour at which the workflow should run (0-23). |
| minute | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The minute at which the workflow should run (0-59). |

<a name='M-JetFlow-WorkflowScheduleBuilder-EveryDays-System-Int32-'></a>
### EveryDays(days) `method`

##### Summary

Schedules the workflow to run every specified number of days. For example, calling EveryDays(2) would schedule the workflow to run every 2 days. The days value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryDays(2).EveryMonths(1) would schedule the workflow to run every 2 days and every month.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| days | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number of days between each execution of the workflow. |

<a name='M-JetFlow-WorkflowScheduleBuilder-EveryHours-System-Int32-'></a>
### EveryHours(hours) `method`

##### Summary

Schedules the workflow to run every specified number of hours. For example, calling EveryHours(2) would schedule the workflow to run every 2 hours. The hours value must be between 1 and 24, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryHours(2).EveryDays(1) would schedule the workflow to run every 2 hours and every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hours | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number of hours between each execution of the workflow. |

<a name='M-JetFlow-WorkflowScheduleBuilder-EveryMinutes-System-Int32-'></a>
### EveryMinutes(minutes) `method`

##### Summary

Schedules the workflow to run every specified number of minutes. For example, calling EveryMinutes(5) would schedule the workflow to run every 5 minutes. The minutes value must be between 1 and 60, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryMinutes(5).EveryHours(1) would schedule the workflow to run every 5 minutes and every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| minutes | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number of minutes between each execution of the workflow. |

<a name='M-JetFlow-WorkflowScheduleBuilder-EveryMonths-System-Int32-'></a>
### EveryMonths(months) `method`

##### Summary

Schedules the workflow to run every specified number of months. For example, calling EveryMonths(3) would schedule the workflow to run every 3 months. The months value must be between 1 and 12, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryMonths(3).EveryDays(1) would schedule the workflow to run every 3 months and every day.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| months | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number of months between each workflow execution. |

<a name='M-JetFlow-WorkflowScheduleBuilder-EverySeconds-System-Int32-'></a>
### EverySeconds(seconds) `method`

##### Summary

Schedules the workflow to run every specified number of seconds. For example, calling EverySeconds(5) would schedule the workflow to run every 5 seconds. The seconds value must be between 1 and 60, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EverySeconds(5).EveryMinutes(1) would schedule the workflow to run every 5 seconds and every minute.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| seconds | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The number of seconds between each execution of the workflow. |

<a name='M-JetFlow-WorkflowScheduleBuilder-MonthlyOn-System-Int32-'></a>
### MonthlyOn(dayOfMonth) `method`

##### Summary

Schedules the workflow to run monthly on a specific day of the month. For example, calling MonthlyOn(15) would schedule the workflow to run on the 15th day of every month. The dayOfMonth value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling MonthlyOn(15).AtHour(14).AtMinute(30) would schedule the workflow to run on the 15th day of every month at 2:30 PM.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| dayOfMonth | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The day of the month on which the workflow should run (1-31). |

<a name='M-JetFlow-WorkflowScheduleBuilder-MonthlyOnAt-System-Int32,System-Int32,System-Int32-'></a>
### MonthlyOnAt(dayOfMonth,hour,minute) `method`

##### Summary

Schedules the workflow to run monthly on a specific day of the month at a specific hour and minute. For example, calling MonthlyOnAt(15, 14, 30) would schedule the workflow to run on the 15th day of every month at 2:30 PM. The dayOfMonth value must be between 1 and 31, the hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling MonthlyOnAt(15, 14, 30).EveryMonths(2) would schedule the workflow to run on the 15th day of every other month at 2:30 PM.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| dayOfMonth | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The day of the month on which the workflow should run (1-31). |
| hour | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The hour at which the workflow should run (0-23). |
| minute | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The minute at which the workflow should run (0-59). |

<a name='M-JetFlow-WorkflowScheduleBuilder-Parse-System-String-'></a>
### Parse(cronString) `method`

##### Summary

Parses a cron string into a workflow schedule builder. The cron string must be in the format of "second minute hour dayOfMonth month dayOfWeek", where each field can be a specific value, a range, a list, or a wildcard. For example, "0 0 * * * *" would represent a schedule that runs every hour at the top of the hour, while "*/5 * * * * *" would represent a schedule that runs every 5 seconds. If the cron string is invalid, a FormatException will be thrown.

##### Returns

A WorkflowScheduleBuilder initialized with the values from the cron string.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| cronString | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The cron string to parse. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.FormatException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.FormatException 'System.FormatException') | Thrown if the cron string is invalid. |

<a name='M-JetFlow-WorkflowScheduleBuilder-WeekDays'></a>
### WeekDays() `method`

##### Summary

Schedules the workflow to run on weekdays (Monday through Friday). This is a convenience method that is equivalent to calling AtDaysOfWeek(new[] { 1, 2, 3, 4, 5 }). This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling WeekDays().EveryHours(1) would schedule the workflow to run on weekdays every hour.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-WorkflowScheduleBuilder-WeekDaysAt-System-Int32,System-Int32-'></a>
### WeekDaysAt(hour,minute) `method`

##### Summary

Schedules the workflow to run on weekdays (Monday through Friday) at a specific hour and minute. This is a convenience method that is equivalent to calling AtDaysOfWeek(new[] { 1, 2, 3, 4, 5 }).AtHour(hour).AtMinute(minute). The hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling WeekDaysAt(9, 30).EveryMonths(1) would schedule the workflow to run on weekdays at 9:30 AM every month.

##### Returns

The current instance of the WorkflowScheduleBuilder, allowing for method chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hour | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The hour at which the workflow should run (0-23). |
| minute | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The minute at which the workflow should run (0-59). |

<a name='T-JetFlow-WorkflowStep'></a>
## WorkflowStep `type`

##### Namespace

JetFlow

##### Summary

Represents a single step within a workflow, including its type, timing, status, and associated data.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Type | [T:JetFlow.WorkflowStep](#T-T-JetFlow-WorkflowStep 'T:JetFlow.WorkflowStep') | The type of the workflow step. Specifies the operation or action performed at this step. |

<a name='M-JetFlow-WorkflowStep-#ctor-JetFlow-WorkflowStepTypes,System-Nullable{System-UInt32},System-String,System-DateTimeOffset,System-DateTimeOffset,JetFlow-WorkflowStepRetry[],System-Nullable{JetFlow-ActivityResultStatus},System-String,System-Object-'></a>
### #ctor(Type,Index,Name,StartTime,EndTime,Retries,Status,ErrorMessage,Result) `constructor`

##### Summary

Represents a single step within a workflow, including its type, timing, status, and associated data.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Type | [JetFlow.WorkflowStepTypes](#T-JetFlow-WorkflowStepTypes 'JetFlow.WorkflowStepTypes') | The type of the workflow step. Specifies the operation or action performed at this step. |
| Index | [System.Nullable{System.UInt32}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{System.UInt32}') | The zero-based index of the step within the workflow sequence, or null if not specified. |
| Name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the workflow step, or null if unnamed. |
| StartTime | [System.DateTimeOffset](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTimeOffset 'System.DateTimeOffset') | The date and time when the step started. |
| EndTime | [System.DateTimeOffset](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTimeOffset 'System.DateTimeOffset') | The date and time when the step ended. |
| Retries | [JetFlow.WorkflowStepRetry[]](#T-JetFlow-WorkflowStepRetry[] 'JetFlow.WorkflowStepRetry[]') | An array of retry attempts for this step, or null if no retries occurred. |
| Status | [System.Nullable{JetFlow.ActivityResultStatus}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Nullable 'System.Nullable{JetFlow.ActivityResultStatus}') | The result status of the step, or null if the status is not set. |
| ErrorMessage | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The error message associated with the step if it failed, or null if no error occurred. |
| Result | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') | The result produced by the step, or null if there is no result. |

<a name='P-JetFlow-WorkflowStep-EndTime'></a>
### EndTime `property`

##### Summary

The date and time when the step ended.

<a name='P-JetFlow-WorkflowStep-ErrorMessage'></a>
### ErrorMessage `property`

##### Summary

The error message associated with the step if it failed, or null if no error occurred.

<a name='P-JetFlow-WorkflowStep-Index'></a>
### Index `property`

##### Summary

The zero-based index of the step within the workflow sequence, or null if not specified.

<a name='P-JetFlow-WorkflowStep-Name'></a>
### Name `property`

##### Summary

The name of the workflow step, or null if unnamed.

<a name='P-JetFlow-WorkflowStep-Result'></a>
### Result `property`

##### Summary

The result produced by the step, or null if there is no result.

<a name='P-JetFlow-WorkflowStep-Retries'></a>
### Retries `property`

##### Summary

An array of retry attempts for this step, or null if no retries occurred.

<a name='P-JetFlow-WorkflowStep-StartTime'></a>
### StartTime `property`

##### Summary

The date and time when the step started.

<a name='P-JetFlow-WorkflowStep-Status'></a>
### Status `property`

##### Summary

The result status of the step, or null if the status is not set.

<a name='P-JetFlow-WorkflowStep-Type'></a>
### Type `property`

##### Summary

The type of the workflow step. Specifies the operation or action performed at this step.

<a name='T-JetFlow-WorkflowStepRetry'></a>
## WorkflowStepRetry `type`

##### Namespace

JetFlow

##### Summary

Represents a retry action for a workflow step, including the retry type and the time the retry occurred.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| RetryType | [T:JetFlow.WorkflowStepRetry](#T-T-JetFlow-WorkflowStepRetry 'T:JetFlow.WorkflowStepRetry') | The type of retry performed for the workflow step. |

<a name='M-JetFlow-WorkflowStepRetry-#ctor-JetFlow-RetryTypes,System-DateTimeOffset-'></a>
### #ctor(RetryType,Timestamp) `constructor`

##### Summary

Represents a retry action for a workflow step, including the retry type and the time the retry occurred.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| RetryType | [JetFlow.RetryTypes](#T-JetFlow-RetryTypes 'JetFlow.RetryTypes') | The type of retry performed for the workflow step. |
| Timestamp | [System.DateTimeOffset](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTimeOffset 'System.DateTimeOffset') | The date and time, in UTC, when the retry action was executed. |

<a name='P-JetFlow-WorkflowStepRetry-RetryType'></a>
### RetryType `property`

##### Summary

The type of retry performed for the workflow step.

<a name='P-JetFlow-WorkflowStepRetry-Timestamp'></a>
### Timestamp `property`

##### Summary

The date and time, in UTC, when the retry action was executed.

<a name='T-JetFlow-WorkflowStepTypes'></a>
## WorkflowStepTypes `type`

##### Namespace

JetFlow

##### Summary

Specifies the type of workflow step, which can be either an action (Activity) or a delay (a pause in the workflow).

<a name='F-JetFlow-WorkflowStepTypes-Action'></a>
### Action `constants`

##### Summary

Represents an action step, typically an activity or task to be executed.

<a name='F-JetFlow-WorkflowStepTypes-Delay'></a>
### Delay `constants`

##### Summary

Gets or sets the delay interval before the operation is executed.
