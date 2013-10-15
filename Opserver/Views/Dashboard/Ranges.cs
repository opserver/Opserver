using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Views.Dashboard
{
    public class Range
        {
            public string Text { get; set; }
            public string Description { get; set; }
            public DateTime Start { get; set; }

            public static List<Range> DefaultRanges
            {
                get
                {
                    return new List<Range>
                        {
                            new Range {Text = "1d", Description = "Today", Start = DateTime.UtcNow.AddDays(-1)},
                            new Range {Text = "1w", Description = "Past week", Start = DateTime.UtcNow.AddDays(-7)},
                            new Range {Text = "1m", Description = "Past Month", Start = DateTime.UtcNow.AddMonths(-1)},
                            new Range {Text = "6m", Description = "Past 6 Months", Start = DateTime.UtcNow.AddMonths(-6)},
                            new Range {Text = "1y", Description = "Past Year", Start = DateTime.UtcNow.AddYears(-1)}
                        };
                }
            }
        }
}