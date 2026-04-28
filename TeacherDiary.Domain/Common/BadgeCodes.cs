namespace TeacherDiary.Domain.Common;

public static class BadgeCodes
{
    public const string FirstBookCompleted = "FIRST_BOOK_COMPLETED";
    public const string Read100Pages = "READ_100_PAGES";
    public const string Complete5Assignments = "COMPLETE_5_ASSIGNMENTS";
    public const string Reach100Points = "REACH_100_POINTS";

    // Streak medals (based on BestStreak)
    public const string Streak3 = "STREAK_3";
    public const string Streak5 = "STREAK_5";
    public const string SevenDayStreak = "SEVEN_DAY_STREAK"; // kept for existing data
    public const string Streak15 = "STREAK_15";
    public const string Streak30 = "STREAK_30";
    public const string Streak45 = "STREAK_45";
    public const string Streak60 = "STREAK_60";
    public const string Streak90 = "STREAK_90";
    public const string Streak180 = "STREAK_180";
    public const string Streak360 = "STREAK_360";

    public static readonly (int Days, string Code)[] StreakTiers =
    [
        (3,   Streak3),
        (5,   Streak5),
        (7,   SevenDayStreak),
        (15,  Streak15),
        (30,  Streak30),
        (45,  Streak45),
        (60,  Streak60),
        (90,  Streak90),
        (180, Streak180),
        (360, Streak360),
    ];

    public static string? GetStreakMedalCode(int bestStreak) => bestStreak switch
    {
        >= 360 => Streak360,
        >= 180 => Streak180,
        >= 90  => Streak90,
        >= 60  => Streak60,
        >= 45  => Streak45,
        >= 30  => Streak30,
        >= 15  => Streak15,
        >= 7   => SevenDayStreak,
        >= 5   => Streak5,
        >= 3   => Streak3,
        _      => null
    };

    // Points medals
    public const string Points100   = "POINTS_100";
    public const string Points250   = "POINTS_250";
    public const string Points500   = "POINTS_500";
    public const string Points1000  = "POINTS_1000";
    public const string Points1500  = "POINTS_1500";
    public const string Points2000  = "POINTS_2000";
    public const string Points3000  = "POINTS_3000";
    public const string Points5000  = "POINTS_5000";
    public const string Points7500  = "POINTS_7500";
    public const string Points10000 = "POINTS_10000";

    public static readonly (int Points, string Code)[] PointsTiers =
    [
        (100,   Points100),
        (250,   Points250),
        (500,   Points500),
        (1000,  Points1000),
        (1500,  Points1500),
        (2000,  Points2000),
        (3000,  Points3000),
        (5000,  Points5000),
        (7500,  Points7500),
        (10000, Points10000),
    ];

    public static string? GetPointsMedalCode(int totalPoints) => totalPoints switch
    {
        >= 10000 => Points10000,
        >= 7500  => Points7500,
        >= 5000  => Points5000,
        >= 3000  => Points3000,
        >= 2000  => Points2000,
        >= 1500  => Points1500,
        >= 1000  => Points1000,
        >= 500   => Points500,
        >= 250   => Points250,
        >= 100   => Points100,
        _        => null
    };
}
