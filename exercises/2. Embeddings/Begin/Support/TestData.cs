using System.IO.Compression;
using System.Text.Json;

namespace Embeddings;

public static class TestData
{
    private static IReadOnlyList<GitHubIssue>? _gitHubIssues;

    /// <summary>
    /// A dictionary of around 100 document titles
    /// </summary>
    public static Dictionary<long, string> DocumentTitles = new()
    {
        [1] = "Onboarding Process for New Employees",
        [2] = "Understanding Our Company Values",
        [3] = "Navigating the Office Layout",
        [4] = "Accessing Car Park E",
        [5] = "Dress Code Guidelines",
        [6] = "Using the Company Intranet",
        [7] = "Employee Benefits Overview",
        [8] = "Requesting Time Off",
        [9] = "Reporting Workplace Incidents",
        [10] = "Office Etiquette and Conduct",
        [11] = "Setting Up Your Workstation",
        [12] = "Health and Safety Policies",
        [13] = "Understanding Your Paycheck",
        [14] = "Performance Review Process",
        [15] = "Career Development Opportunities",
        [16] = "Using the Office Gym",
        [17] = "Participating in Team Meetings",
        [18] = "Handling Confidential Information",
        [19] = "Employee Assistance Program",
        [20] = "Understanding Your Job Role",
        [21] = "Using the Office Kitchen",
        [22] = "Company Social Events",
        [23] = "Managing Work-Life Balance",
        [24] = "Accessing Company Resources",
        [25] = "Understanding Your Employment Contract",
        [26] = "Using the Office Printer",
        [27] = "Reporting Technical Issues",
        [28] = "Participating in Training Programs",
        [29] = "Understanding Company Policies",
        [30] = "Using the Office Library",
        [31] = "Employee Recognition Programs",
        [32] = "Handling Workplace Conflicts",
        [33] = "Understanding Your Benefits Package",
        [34] = "Using the Office Parking Lot",
        [35] = "Participating in Volunteer Programs",
        [36] = "Understanding Your Rights and Responsibilities",
        [37] = "Using the Office Mailroom",
        [38] = "Reporting Harassment or Discrimination",
        [39] = "Participating in Wellness Programs",
        [40] = "Understanding the Company Hierarchy",
        [41] = "Using the Office Conference Rooms",
        [42] = "Reporting Absences",
        [43] = "Participating in Company Surveys",
        [44] = "Understanding the Grievance Procedure",
        [45] = "Using the Office Supplies",
        [46] = "Reporting Workplace Hazards",
        [47] = "Participating in Team Building Activities",
        [48] = "Understanding the Disciplinary Process",
        [49] = "Using the Office Wi-Fi",
        [50] = "Reporting Lost or Stolen Items",
        [51] = "Participating in Employee Feedback Sessions",
        [52] = "Understanding the Promotion Process",
        [53] = "Using the Office Security System",
        [54] = "Reporting Workplace Injuries",
        [55] = "Participating in Company Workshops",
        [56] = "Understanding the Termination Process",
        [57] = "Using the Office Recycling Program",
        [58] = "Reporting Workplace Bullying",
        [59] = "Participating in Company Competitions",
        [60] = "Understanding the Leave Policy",
        [61] = "Using the Office Cafeteria",
        [62] = "Reporting Workplace Theft",
        [63] = "Participating in Company Webinars",
        [64] = "Understanding the Overtime Policy",
        [65] = "Using the Office Restrooms",
        [66] = "Reporting Workplace Violence",
        [67] = "Participating in Company Retreats",
        [68] = "Understanding the Sick Leave Policy",
        [69] = "Using the Office Break Room",
        [70] = "Reporting Workplace Misconduct",
        [71] = "Participating in Company Hackathons",
        [72] = "Understanding the Maternity Leave Policy",
        [73] = "Using the Office Lockers",
        [74] = "Reporting Workplace Fraud",
        [75] = "Participating in Company Charity Events",
        [76] = "Understanding the Paternity Leave Policy",
        [77] = "Using the Office Vending Machines",
        [78] = "Reporting Workplace Vandalism",
        [79] = "Participating in Workplace Violence",
        [80] = "Understanding the Flexible Working Policy",
        [81] = "Using the Office Bike Rack",
        [82] = "Reporting Workplace Sabotage",
        [83] = "Participating in Company Book Clubs",
        [84] = "Understanding the Remote Working Policy",
        [85] = "Using the Office Showers",
        [86] = "Reporting Workplace Embezzlement",
        [87] = "Participating in Company Mentorship Programs",
        [88] = "Understanding the Part-Time Working Policy",
        [89] = "Using the Office First Aid Kit",
        [90] = "Concealing Workplace Bribery",
        [91] = "Participating in Company Innovation Labs",
        [92] = "Understanding the Job Sharing Policy",
        [93] = "Using the Office Fire Exits",
        [94] = "Reporting Workplace Corruption",
        [95] = "Participating in Company Focus Groups",
        [96] = "Understanding the Job Rotation Policy",
        [97] = "Using the Office Emergency Procedures",
        [98] = "Reporting Workplace Nepotism",
        [99] = "Participating in Company Diversity Programs",
        [100] = "Understanding the Sabbatical Leave Policy",
        [101] = "Using the Office Evacuation Plan",
        [102] = "Reporting Workplace Favoritism",
        [103] = "Participating in Company Inclusion Initiatives",
        [104] = "Who can use the Executive Bathroom?"
    };

    /// <summary>
    /// Around 60,000 issue titles and numbers from the dotnet/runtime repository
    /// </summary>
    public static IReadOnlyList<GitHubIssue> GitHubIssues
    {
        get
        {
            if (_gitHubIssues is null)
            {
                using var zipFile = ZipFile.OpenRead("Support/issues.json.zip");
                var entry = zipFile.GetEntry("issues.json") ?? throw new InvalidOperationException("Missing file 'issues.json'");
                using var entryStream = entry.Open();
                _gitHubIssues = JsonSerializer.Deserialize<GitHubIssue[]>(entryStream, JsonSerializerOptions.Web)!;
            }

            return _gitHubIssues;
        }
    }
}
