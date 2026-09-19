using JetFlow.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

public record ActivityPerformanceRecordEvent(string? namespaceName, ActivityPerformanceRecord PerformanceRecord);
public record WorkflowPerformanceRecordEvent(string? namespaceName, WorkflowPerformanceRecord PerformanceRecord);
