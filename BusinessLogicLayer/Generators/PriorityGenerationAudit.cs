using static BusinessLogicLayer.Generators.PriorityScheduleRules;

namespace BusinessLogicLayer.Generators;

// Explicitly opt-in test/offline instrumentation; production entry points never supply it.
internal sealed class PriorityGenerationAudit
{
    internal Dictionary<(int Employee, int Day), Interval> FixedAnchors { get; } = [];
    internal bool CollectDiagnostics { get; init; } = true;
    internal IReadOnlyList<Assignment>? Assignments { get; set; }
    internal Action? BeforeRestart { get; init; }
    public int EarlyCandidates { get; set; }
    public int LateCandidates { get; set; }
    public int EarlyEvaluations { get; set; }
    public int LateEvaluations { get; set; }
    public int FeasibleEarlyEvaluations { get; set; }
    public int FeasibleLateEvaluations { get; set; }
    public Dictionary<string, int> Rejections { get; } = [];
    public int AvailabilityBlockedDays { get; set; }
    public int WeekendPolicyBlockedDays { get; set; }
    public int ExternalBlockedDays { get; set; }
    public int UnavailableFixedAnchors { get; set; }
    public int NonImprovingTrials { get; set; }
    public int EarlyToLateChanges { get; set; }
    public int LateToEarlyChanges { get; set; }
    public int Trials { get; set; }
    public int PairedTrials { get; set; }
    public int AcceptedPairedTrials { get; set; }
    public int CoordinatedTrials { get; set; }
    public int CoordinatedEvaluations { get; set; }
    public bool CoordinatedBudgetExhausted { get; set; }
    public int RestartTrials { get; set; }
    public int LegacyGuidanceBuilds { get; set; }
    public int AcceptedRestarts { get; set; }
    public int OwnershipTrials { get; set; }
    public int OwnershipTransfers { get; set; }
    public List<double[]> RestartObjectives { get; } = [];
    public List<PriorityAnchorProbe> AnchorProbes { get; } = [];
    public int EquivalentLaneTrialsSkipped { get; set; }
    public int RestartProjectionTrials { get; set; }
    public int RestartLookaheadTrials { get; set; }
    public int RootCandidateEvaluations { get; set; }
    public int RankingCandidateEvaluations { get; set; }
    public int RestartCandidateEvaluations { get; set; }
    public double RestartRankingMilliseconds { get; set; }
    public List<int> RestartSelectedDays { get; } = [];
    public List<PriorityAnchorProbe> RestartProjectionScores { get; } = [];
    public List<PriorityAnchorProbe> RestartLookaheadScores { get; } = [];
    public double[]? BeforeRestarts { get; set; }
    public int Evaluations { get; set; }
    public bool BudgetExhausted { get; set; }
    public bool TrialLimitReached { get; set; }
    public double[]? BeforeReorientation { get; set; }
    public double[]? AfterReorientation { get; set; }
    public double[]? FinalObjective { get; set; }

    internal void Reject(string reason) => Rejections[reason] = Rejections.GetValueOrDefault(reason) + 1;

    internal void Reset()
    {
        Assignments = null;
        EarlyCandidates = LateCandidates = EarlyEvaluations = LateEvaluations = FeasibleEarlyEvaluations = FeasibleLateEvaluations = 0;
        AvailabilityBlockedDays = WeekendPolicyBlockedDays = ExternalBlockedDays = UnavailableFixedAnchors = NonImprovingTrials = 0;
        EarlyToLateChanges = LateToEarlyChanges = Trials = PairedTrials = AcceptedPairedTrials = 0;
        CoordinatedTrials = CoordinatedEvaluations = Evaluations = RestartTrials = AcceptedRestarts = LegacyGuidanceBuilds = 0;
        OwnershipTrials = OwnershipTransfers = 0;
        EquivalentLaneTrialsSkipped = 0;
        RestartProjectionTrials = RestartLookaheadTrials = 0; RestartRankingMilliseconds = 0;
        RootCandidateEvaluations = RankingCandidateEvaluations = RestartCandidateEvaluations = 0;
        CoordinatedBudgetExhausted = BudgetExhausted = TrialLimitReached = false;
        BeforeRestarts = BeforeReorientation = AfterReorientation = FinalObjective = null;
        Rejections.Clear(); RestartObjectives.Clear(); AnchorProbes.Clear();
        RestartSelectedDays.Clear();
        RestartProjectionScores.Clear(); RestartLookaheadScores.Clear();
    }
}

internal sealed record PriorityAnchorProbe(int Day, int Start, double[] Quality);
