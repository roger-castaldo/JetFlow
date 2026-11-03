using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow.UI;

public record struct DateRange(DateTimeOffset? Start, DateTimeOffset? End);
