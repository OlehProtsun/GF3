using System.Globalization;
using System.Text;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Services.Export;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Builds portable graph/container exports without leaking internal database ids.
/// The exporter reconstructs the domain graph from business services, then emits SQL/CSV
/// that can be imported into another database instance using natural business keys.
/// </summary>
public sealed class GraphExportService : IGraphExportService
{
    /// <summary>
    /// A fully-hydrated export unit for one graph. Keeping all related entities together makes
    /// the SQL writer deterministic and prevents repeated service calls while emitting statements.
    /// </summary>
    private sealed record GraphSqlBundle(
        ContainerModel Container,
        ShopModel Shop,
        ScheduleModel Graph,
        IReadOnlyList<EmployeeModel> ReferencedEmployees,
        IReadOnlyList<ScheduleEmployeeModel> ScheduleEmployees,
        IReadOnlyList<ScheduleSlotModel> Slots,
        IReadOnlyList<ScheduleCellStyleModel> Styles,
        AvailabilityGroupModel? Group,
        IReadOnlyList<AvailabilityGroupMemberModel> Members,
        IReadOnlyList<AvailabilityGroupDayModel> Days,
        string? ExportNote);

    /// <summary>
    /// Container exports may include many graphs that point to the same container, shop, employees,
    /// or availability group. The tracker ensures we emit "insert if missing" statements only once
    /// per logical business entity.
    /// </summary>
    private sealed class ExportEmissionTracker
    {
        public HashSet<string> Containers { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Shops { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Employees { get; } = new(StringComparer.Ordinal);
        public HashSet<string> AvailabilityGroups { get; } = new(StringComparer.Ordinal);
        public HashSet<string> AvailabilityGroupMembers { get; } = new(StringComparer.Ordinal);
        public HashSet<string> AvailabilityGroupDays { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Schedules { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ScheduleEmployees { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ScheduleSlots { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ScheduleCellStyles { get; } = new(StringComparer.Ordinal);
    }

    private readonly IAvailabilityGroupService _availabilityGroupService;
    private readonly IContainerService _containerService;
    private readonly IEmployeeService _employeeService;
    private readonly IShopService _shopService;

    public GraphExportService(
        IContainerService containerService,
        IAvailabilityGroupService availabilityGroupService,
        IEmployeeService employeeService,
        IShopService shopService)
    {
        _containerService = containerService;
        _availabilityGroupService = availabilityGroupService;
        _employeeService = employeeService;
        _shopService = shopService;
    }

    public async Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
    {
        var bundle = await LoadGraphSqlBundleAsync(containerId, graphId, includeEmployees, includeStyles, ct).ConfigureAwait(false);
        var relatedGraphSources = await LoadRelatedGraphHintSourcesAsync(containerId, bundle.Graph, ct).ConfigureAwait(false);
        bundle = bundle with
        {
            ExportNote = BuildExportNote(bundle.Graph, bundle.ScheduleEmployees, bundle.Slots, relatedGraphSources),
        };
        var script = BuildSqlScript([bundle], includeEmployees, includeStyles, $"GF3 Graph export ({bundle.Graph.Name})");
        return Encoding.UTF8.GetBytes(script);
    }

    public async Task<byte[]> ExportContainerSqlAsync(int containerId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
    {
        var container = await _containerService.GetAsync(containerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {containerId} was not found.");

        var graphs = await _containerService.GetGraphsAsync(containerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {containerId} was not found.");
        var bundles = new List<GraphSqlBundle>(graphs.Count);

        foreach (var graph in graphs.OrderBy(x => x.Id))
        {
            bundles.Add(await LoadGraphSqlBundleAsync(container, graph, includeEmployees, includeStyles, ct).ConfigureAwait(false));
        }

        for (var index = 0; index < bundles.Count; index++)
        {
            var bundle = bundles[index];
            var relatedGraphSources = bundles
                .Where(relatedBundle => relatedBundle.Graph.Id != bundle.Graph.Id)
                .Select(relatedBundle => new GraphRelatedScheduleHintSource(relatedBundle.Graph, relatedBundle.Slots))
                .ToList();

            bundles[index] = bundle with
            {
                ExportNote = BuildExportNote(bundle.Graph, bundle.ScheduleEmployees, bundle.Slots, relatedGraphSources),
            };
        }

        var script = BuildSqlScript(bundles, includeEmployees, includeStyles, $"GF3 Container export ({container.Name})");
        return Encoding.UTF8.GetBytes(script);
    }

    public async Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)
    {
        var graph = await _containerService.GetGraphByIdAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        var slots = await _containerService.GetGraphSlotsAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");
        var employees = includeEmployees
            ? await _containerService.GetGraphEmployeesAsync(containerId, graphId, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.")
            : [];
        var styles = includeStyles
            ? await _containerService.GetGraphCellStylesAsync(containerId, graphId, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.")
            : [];

        var employeeById = employees
            .Where(x => x.Employee is not null)
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.First().Employee!);

        var styleByKey = styles.ToDictionary(x => (x.DayOfMonth, x.EmployeeId), x => x);

        var sb = new StringBuilder(16_384);
        sb.AppendLine("ScheduleId,ScheduleName,Year,Month,Day,SlotNo,FromTime,ToTime,EmployeeId,EmployeeName,Status,BackgroundColorArgb,TextColorArgb");

        foreach (var slot in slots.OrderBy(s => s.DayOfMonth).ThenBy(s => s.SlotNo).ThenBy(s => s.FromTime))
        {
            var employeeName = string.Empty;
            if (slot.EmployeeId is int employeeId && employeeById.TryGetValue(employeeId, out var employee))
            {
                employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            }

            styleByKey.TryGetValue((slot.DayOfMonth, slot.EmployeeId ?? 0), out var style);

            sb.Append(EscapeCsv(graph.Id)).Append(',')
                .Append(EscapeCsv(graph.Name)).Append(',')
                .Append(EscapeCsv(graph.Year)).Append(',')
                .Append(EscapeCsv(graph.Month)).Append(',')
                .Append(EscapeCsv(slot.DayOfMonth)).Append(',')
                .Append(EscapeCsv(slot.SlotNo)).Append(',')
                .Append(EscapeCsv(slot.FromTime)).Append(',')
                .Append(EscapeCsv(slot.ToTime)).Append(',')
                .Append(EscapeCsv(slot.EmployeeId)).Append(',')
                .Append(EscapeCsv(employeeName)).Append(',')
                .Append(EscapeCsv(slot.Status.ToString())).Append(',')
                .Append(EscapeCsv(style?.BackgroundColorArgb)).Append(',')
                .Append(EscapeCsv(style?.TextColorArgb))
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<GraphSqlBundle> LoadGraphSqlBundleAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct)
    {
        var graph = await _containerService.GetGraphByIdAsync(containerId, graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        var container = await _containerService.GetAsync(graph.ContainerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {graph.ContainerId} was not found.");

        return await LoadGraphSqlBundleAsync(container, graph, includeEmployees, includeStyles, ct).ConfigureAwait(false);
    }

    private async Task<GraphSqlBundle> LoadGraphSqlBundleAsync(
        ContainerModel container,
        ScheduleModel graph,
        bool includeEmployees,
        bool includeStyles,
        CancellationToken ct)
    {
        var shop = await _shopService.GetAsync(graph.ShopId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shop with id {graph.ShopId} was not found.");

        var scheduleEmployees = includeEmployees
            ? await _containerService.GetGraphEmployeesAsync(container.Id, graph.Id, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Graph with id {graph.Id} was not found.")
            : [];

        var slots = await _containerService.GetGraphSlotsAsync(container.Id, graph.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graph.Id} was not found.");
        var styles = includeStyles
            ? await _containerService.GetGraphCellStylesAsync(container.Id, graph.Id, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Graph with id {graph.Id} was not found.")
            : [];

        AvailabilityGroupModel? group = null;
        List<AvailabilityGroupMemberModel> members = [];
        List<AvailabilityGroupDayModel> days = [];

        if (graph.AvailabilityGroupId is int availabilityGroupId && availabilityGroupId > 0)
        {
            var full = await _availabilityGroupService.LoadFullAsync(availabilityGroupId, ct).ConfigureAwait(false);
            group = full.group;
            members = full.members;
            days = full.days;
        }

        var referencedEmployees = await ResolveReferencedEmployeesAsync(scheduleEmployees, slots, styles, members, ct).ConfigureAwait(false);

        return new GraphSqlBundle(
            container,
            shop,
            graph,
            referencedEmployees,
            scheduleEmployees,
            slots,
            styles,
            group,
            members,
            days,
            graph.Note);
    }

    private async Task<IReadOnlyList<EmployeeModel>> ResolveReferencedEmployeesAsync(
        IReadOnlyList<ScheduleEmployeeModel> scheduleEmployees,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<ScheduleCellStyleModel> styles,
        IReadOnlyList<AvailabilityGroupMemberModel> members,
        CancellationToken ct)
    {
        var employeesById = new Dictionary<int, EmployeeModel>();
        var missingIds = new HashSet<int>();

        // Export payloads are allowed to be partially hydrated. For example, a slot may know only
        // EmployeeId while ScheduleEmployee contains the full Employee object. We collect everything
        // that is already present and perform a final lazy load only for missing ids.
        static void TrackEmployee(Dictionary<int, EmployeeModel> target, HashSet<int> missing, EmployeeModel? employee, int? fallbackId)
        {
            if (employee is not null && employee.Id > 0)
            {
                target[employee.Id] = employee;
                return;
            }

            if (fallbackId is int id && id > 0 && !target.ContainsKey(id))
            {
                missing.Add(id);
            }
        }

        foreach (var scheduleEmployee in scheduleEmployees)
        {
            TrackEmployee(employeesById, missingIds, scheduleEmployee.Employee, scheduleEmployee.EmployeeId);
        }

        foreach (var slot in slots)
        {
            TrackEmployee(employeesById, missingIds, slot.Employee, slot.EmployeeId);
        }

        foreach (var style in styles)
        {
            TrackEmployee(employeesById, missingIds, null, style.EmployeeId);
        }

        foreach (var member in members)
        {
            TrackEmployee(employeesById, missingIds, member.Employee, member.EmployeeId);
        }

        if (missingIds.Count > 0)
        {
            var fetchedEmployees = await Task.WhenAll(missingIds.Select(id => _employeeService.GetAsync(id, ct))).ConfigureAwait(false);
            foreach (var fetchedEmployee in fetchedEmployees)
            {
                if (fetchedEmployee is null || fetchedEmployee.Id <= 0)
                {
                    throw new KeyNotFoundException("A referenced employee could not be loaded for SQL export.");
                }

                employeesById[fetchedEmployee.Id] = fetchedEmployee;
            }
        }

        return employeesById.Values
            .OrderBy(employee => employee.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(employee => employee.Id)
            .ToList();
    }

    private async Task<List<GraphRelatedScheduleHintSource>> LoadRelatedGraphHintSourcesAsync(int containerId, ScheduleModel graph, CancellationToken ct)
    {
        var graphs = await _containerService.GetGraphsAsync(containerId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Container with id {containerId} was not found.");
        var relatedGraphSources = new List<GraphRelatedScheduleHintSource>();

        foreach (var relatedGraph in graphs.Where(relatedGraph =>
                     relatedGraph.Id != graph.Id
                     && relatedGraph.Year == graph.Year
                     && relatedGraph.Month == graph.Month))
        {
            var relatedSlots = await _containerService.GetGraphSlotsAsync(containerId, relatedGraph.Id, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Graph with id {relatedGraph.Id} was not found.");

            relatedGraphSources.Add(new GraphRelatedScheduleHintSource(relatedGraph, relatedSlots));
        }

        return relatedGraphSources;
    }

    private static string? BuildExportNote(
        ScheduleModel graph,
        IReadOnlyList<ScheduleEmployeeModel> scheduleEmployees,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<GraphRelatedScheduleHintSource> relatedGraphSources)
    {
        var noteMetadata = GraphNoteExportMetadataParser.Parse(graph.Note);
        var dynamicTextCells = GraphRelatedScheduleHintExportBuilder.BuildTextCells(
            graph,
            scheduleEmployees,
            slots,
            noteMetadata.TextCells,
            relatedGraphSources);
        var effectiveTextCells = GraphRelatedScheduleHintExportBuilder.MergeTextCells(noteMetadata.TextCells, dynamicTextCells);
        return GraphNoteSqlExportBuilder.BuildPortableNote(graph.Note, effectiveTextCells);
    }

    private static string BuildSqlScript(
        IReadOnlyList<GraphSqlBundle> bundles,
        bool includeEmployees,
        bool includeStyles,
        string title)
    {
        var sb = new StringBuilder(64_000);
        var tracker = new ExportEmissionTracker();

        sb.AppendLine($"-- {title}");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- Portable GF3 export. Compatible with Database page importer.");
        sb.AppendLine("-- Missing entities are inserted; existing ones are skipped.");
        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();

        foreach (var bundle in bundles)
        {
            AppendBundleStatements(sb, bundle, tracker, includeEmployees, includeStyles);
        }

        sb.AppendLine("COMMIT;");
        return sb.ToString();
    }

    /// <summary>
    /// Emits one graph bundle into the SQL script.
    /// The order matters: referenced roots (container/shop/employees/group) must appear before
    /// graph rows, and graph rows must exist before schedule employees, slots, and styles.
    /// </summary>
    private static void AppendBundleStatements(
        StringBuilder sb,
        GraphSqlBundle bundle,
        ExportEmissionTracker tracker,
        bool includeEmployees,
        bool includeStyles)
    {
        var employeeById = bundle.ReferencedEmployees.ToDictionary(employee => employee.Id);

        sb.AppendLine($"-- Schedule: {bundle.Graph.Name} ({bundle.Graph.Year:D4}-{bundle.Graph.Month:D2})");

        AppendIfNew(sb, tracker.Containers, BuildContainerKey(bundle.Container), BuildContainerInsert(bundle.Container));
        AppendIfNew(sb, tracker.Shops, BuildShopKey(bundle.Shop), BuildShopInsert(bundle.Shop));

        foreach (var employee in bundle.ReferencedEmployees)
        {
            AppendIfNew(sb, tracker.Employees, BuildEmployeeKey(employee), BuildEmployeeInsert(employee));
        }

        if (bundle.Group is not null)
        {
            AppendIfNew(sb, tracker.AvailabilityGroups, BuildAvailabilityGroupKey(bundle.Group), BuildAvailabilityGroupInsert(bundle.Group));

            foreach (var member in bundle.Members.OrderBy(x => x.DisplayOrder).ThenBy(x => x.EmployeeId))
            {
                if (!employeeById.TryGetValue(member.EmployeeId, out var employee))
                {
                    throw new KeyNotFoundException($"Availability group member employee with id {member.EmployeeId} was not loaded for SQL export.");
                }

                AppendIfNew(
                    sb,
                    tracker.AvailabilityGroupMembers,
                    BuildAvailabilityGroupMemberKey(bundle.Group, employee),
                    BuildAvailabilityGroupMemberInsert(bundle.Group, employee, member));
            }

            foreach (var day in bundle.Days.OrderBy(x => x.DayOfMonth).ThenBy(x => x.AvailabilityGroupMemberId))
            {
                var member = bundle.Members.FirstOrDefault(x => x.Id == day.AvailabilityGroupMemberId)
                    ?? throw new KeyNotFoundException($"Availability group member with id {day.AvailabilityGroupMemberId} was not found in export payload.");

                if (!employeeById.TryGetValue(member.EmployeeId, out var employee))
                {
                    throw new KeyNotFoundException($"Availability group day employee with id {member.EmployeeId} was not loaded for SQL export.");
                }

                AppendIfNew(
                    sb,
                    tracker.AvailabilityGroupDays,
                    BuildAvailabilityGroupDayKey(bundle.Group, employee, day),
                    BuildAvailabilityGroupDayInsert(bundle.Group, employee, day));
            }
        }

        AppendIfNew(sb, tracker.Schedules, BuildScheduleKey(bundle.Container, bundle.Shop, bundle.Graph), BuildScheduleInsert(bundle));

        if (includeEmployees)
        {
            foreach (var scheduleEmployee in bundle.ScheduleEmployees.OrderBy(x => x.DisplayOrder).ThenBy(x => x.EmployeeId))
            {
                if (!employeeById.TryGetValue(scheduleEmployee.EmployeeId, out var employee))
                {
                    throw new KeyNotFoundException($"Schedule employee with id {scheduleEmployee.EmployeeId} was not loaded for SQL export.");
                }

                AppendIfNew(
                    sb,
                    tracker.ScheduleEmployees,
                    BuildScheduleEmployeeKey(bundle.Container, bundle.Shop, bundle.Graph, employee),
                    BuildScheduleEmployeeInsert(bundle.Container, bundle.Shop, bundle.Graph, employee, scheduleEmployee));
            }
        }

        foreach (var slot in bundle.Slots.OrderBy(x => x.DayOfMonth).ThenBy(x => x.SlotNo).ThenBy(x => x.FromTime).ThenBy(x => x.ToTime))
        {
            EmployeeModel? employee = null;
            if (slot.EmployeeId is int employeeId && employeeId > 0)
            {
                if (!employeeById.TryGetValue(employeeId, out employee))
                {
                    throw new KeyNotFoundException($"Assigned slot employee with id {employeeId} was not loaded for SQL export.");
                }
            }

            AppendIfNew(
                sb,
                tracker.ScheduleSlots,
                BuildScheduleSlotKey(bundle.Container, bundle.Shop, bundle.Graph, slot),
                BuildScheduleSlotInsert(bundle.Container, bundle.Shop, bundle.Graph, slot, employee));
        }

        if (includeStyles)
        {
            foreach (var style in bundle.Styles.OrderBy(x => x.DayOfMonth).ThenBy(x => x.EmployeeId))
            {
                if (!employeeById.TryGetValue(style.EmployeeId, out var employee))
                {
                    throw new KeyNotFoundException($"Schedule cell style employee with id {style.EmployeeId} was not loaded for SQL export.");
                }

                AppendIfNew(
                    sb,
                    tracker.ScheduleCellStyles,
                    BuildScheduleCellStyleKey(bundle.Container, bundle.Shop, bundle.Graph, employee, style),
                    BuildScheduleCellStyleInsert(bundle.Container, bundle.Shop, bundle.Graph, employee, style));
            }
        }

        sb.AppendLine();
    }

    private static void AppendIfNew(StringBuilder sb, HashSet<string> seenKeys, string key, string statement)
    {
        if (!seenKeys.Add(key))
        {
            return;
        }

        sb.AppendLine(statement);
    }

    private static string BuildContainerInsert(ContainerModel container)
        => SqlInsertIfMissing(
            "container",
            [
                ("name", ToSqlLiteral(container.Name)),
                ("note", ToSqlLiteral(container.Note))
            ],
            $"name = {ToSqlLiteral(container.Name)}");

    private static string BuildShopInsert(ShopModel shop)
        => SqlInsertIfMissing(
            "shop",
            [
                ("name", ToSqlLiteral(shop.Name)),
                ("address", ToSqlLiteral(shop.Address)),
                ("description", ToSqlLiteral(shop.Description))
            ],
            $"name = {ToSqlLiteral(shop.Name)}");

    private static string BuildEmployeeInsert(EmployeeModel employee)
        => SqlInsertIfMissing(
            "employee",
            [
                ("first_name", ToSqlLiteral(employee.FirstName)),
                ("last_name", ToSqlLiteral(employee.LastName)),
                ("phone", ToSqlLiteral(employee.Phone)),
                ("email", ToSqlLiteral(employee.Email))
            ],
            $"first_name = {ToSqlLiteral(employee.FirstName)} AND last_name = {ToSqlLiteral(employee.LastName)}");

    private static string BuildAvailabilityGroupInsert(AvailabilityGroupModel group)
        => SqlInsertIfMissing(
            "availability_group",
            [
                ("name", ToSqlLiteral(group.Name)),
                ("year", ToSqlLiteral(group.Year)),
                ("month", ToSqlLiteral(group.Month))
            ],
            $"year = {group.Year} AND month = {group.Month} AND name = {ToSqlLiteral(group.Name)}");

    private static string BuildAvailabilityGroupMemberInsert(AvailabilityGroupModel group, EmployeeModel employee, AvailabilityGroupMemberModel member)
    {
        var groupRef = BuildAvailabilityGroupRef(group);
        var employeeRef = BuildEmployeeRef(employee);

        return SqlInsertIfMissing(
            "availability_group_member",
            [
                ("availability_group_id", groupRef),
                ("employee_id", employeeRef),
                ("display_order", ToSqlLiteral(member.DisplayOrder))
            ],
            $"availability_group_id = {groupRef} AND employee_id = {employeeRef}",
            [$"{groupRef} IS NOT NULL", $"{employeeRef} IS NOT NULL"]);
    }

    private static string BuildAvailabilityGroupDayInsert(AvailabilityGroupModel group, EmployeeModel employee, AvailabilityGroupDayModel day)
    {
        var memberRef = BuildAvailabilityGroupMemberRef(group, employee);

        return SqlInsertIfMissing(
            "availability_group_day",
            [
                ("availability_group_member_id", memberRef),
                ("day_of_month", ToSqlLiteral(day.DayOfMonth)),
                ("kind", ToSqlLiteral(day.Kind.ToString())),
                ("interval_str", ToSqlLiteral(day.IntervalStr))
            ],
            $"availability_group_member_id = {memberRef} AND day_of_month = {day.DayOfMonth}",
            [$"{memberRef} IS NOT NULL"]);
    }

    private static string BuildScheduleInsert(GraphSqlBundle bundle)
    {
        var containerRef = BuildContainerRef(bundle.Container);
        var shopRef = BuildShopRef(bundle.Shop);
        var groupRef = bundle.Group is null ? "NULL" : BuildAvailabilityGroupRef(bundle.Group);

        var guardExpressions = new List<string>
        {
            $"{containerRef} IS NOT NULL",
            $"{shopRef} IS NOT NULL",
        };

        if (bundle.Group is not null)
        {
            guardExpressions.Add($"{groupRef} IS NOT NULL");
        }

        return SqlInsertIfMissing(
            "schedule",
            [
                ("container_id", containerRef),
                ("shop_id", shopRef),
                ("name", ToSqlLiteral(bundle.Graph.Name)),
                ("year", ToSqlLiteral(bundle.Graph.Year)),
                ("month", ToSqlLiteral(bundle.Graph.Month)),
                ("people_per_shift", ToSqlLiteral(bundle.Graph.PeoplePerShift)),
                ("shift1_time", ToSqlLiteral(bundle.Graph.Shift1Time)),
                ("shift2_time", ToSqlLiteral(bundle.Graph.Shift2Time)),
                ("max_hours_per_emp_month", ToSqlLiteral(bundle.Graph.MaxHoursPerEmpMonth)),
                ("max_consecutive_days", ToSqlLiteral(bundle.Graph.MaxConsecutiveDays)),
                ("max_consecutive_full", ToSqlLiteral(bundle.Graph.MaxConsecutiveFull)),
                ("max_full_per_month", ToSqlLiteral(bundle.Graph.MaxFullPerMonth)),
                ("note", ToSqlLiteral(bundle.ExportNote)),
                ("availability_group_id", groupRef)
            ],
            BuildScheduleExistsCondition(bundle.Container, bundle.Shop, bundle.Graph),
            guardExpressions);
    }

    private static string BuildScheduleEmployeeInsert(
        ContainerModel container,
        ShopModel shop,
        ScheduleModel graph,
        EmployeeModel employee,
        ScheduleEmployeeModel scheduleEmployee)
    {
        var scheduleRef = BuildScheduleRef(container, shop, graph);
        var employeeRef = BuildEmployeeRef(employee);

        return SqlInsertIfMissing(
            "schedule_employee",
            [
                ("schedule_id", scheduleRef),
                ("employee_id", employeeRef),
                ("min_hours_month", ToSqlLiteral(scheduleEmployee.MinHoursMonth)),
                ("display_order", ToSqlLiteral(scheduleEmployee.DisplayOrder))
            ],
            $"schedule_id = {scheduleRef} AND employee_id = {employeeRef}",
            [$"{scheduleRef} IS NOT NULL", $"{employeeRef} IS NOT NULL"]);
    }

    private static string BuildScheduleSlotInsert(
        ContainerModel container,
        ShopModel shop,
        ScheduleModel graph,
        ScheduleSlotModel slot,
        EmployeeModel? employee)
    {
        var scheduleRef = BuildScheduleRef(container, shop, graph);
        var employeeRef = employee is null ? "NULL" : BuildEmployeeRef(employee);
        var guardExpressions = new List<string> { $"{scheduleRef} IS NOT NULL" };

        if (employee is not null)
        {
            guardExpressions.Add($"{employeeRef} IS NOT NULL");
        }

        return SqlInsertIfMissing(
            "schedule_slot",
            [
                ("schedule_id", scheduleRef),
                ("day_of_month", ToSqlLiteral(slot.DayOfMonth)),
                ("slot_no", ToSqlLiteral(slot.SlotNo)),
                ("employee_id", employeeRef),
                ("status", ToSqlLiteral(slot.Status.ToString())),
                ("from_time", ToSqlLiteral(slot.FromTime)),
                ("to_time", ToSqlLiteral(slot.ToTime))
            ],
            $"schedule_id = {scheduleRef} AND day_of_month = {slot.DayOfMonth} AND slot_no = {slot.SlotNo} AND from_time = {ToSqlLiteral(slot.FromTime)} AND to_time = {ToSqlLiteral(slot.ToTime)}",
            guardExpressions);
    }

    private static string BuildScheduleCellStyleInsert(
        ContainerModel container,
        ShopModel shop,
        ScheduleModel graph,
        EmployeeModel employee,
        ScheduleCellStyleModel style)
    {
        var scheduleRef = BuildScheduleRef(container, shop, graph);
        var employeeRef = BuildEmployeeRef(employee);

        return SqlInsertIfMissing(
            "schedule_cell_style",
            [
                ("schedule_id", scheduleRef),
                ("day_of_month", ToSqlLiteral(style.DayOfMonth)),
                ("employee_id", employeeRef),
                ("background_color_argb", ToSqlLiteral(style.BackgroundColorArgb)),
                ("text_color_argb", ToSqlLiteral(style.TextColorArgb))
            ],
            $"schedule_id = {scheduleRef} AND day_of_month = {style.DayOfMonth} AND employee_id = {employeeRef}",
            [$"{scheduleRef} IS NOT NULL", $"{employeeRef} IS NOT NULL"]);
    }

    private static string SqlInsertIfMissing(
        string table,
        IReadOnlyList<(string Column, string Expression)> values,
        string existsPredicate,
        IReadOnlyList<string>? guardExpressions = null)
    {
        var columns = string.Join(", ", values.Select(v => v.Column));
        var selectValues = string.Join(", ", values.Select(v => v.Expression));

        var whereClauses = new List<string>();
        if (guardExpressions is not null)
        {
            whereClauses.AddRange(guardExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)));
        }

        whereClauses.Add($"NOT EXISTS (SELECT 1 FROM {table} WHERE {existsPredicate})");

        return $"INSERT OR IGNORE INTO {table} ({columns}) SELECT {selectValues} WHERE {string.Join(" AND ", whereClauses)};";
    }

    private static string BuildContainerRef(ContainerModel container)
        => $"(SELECT id FROM container WHERE name = {ToSqlLiteral(container.Name)})";

    private static string BuildShopRef(ShopModel shop)
        => $"(SELECT id FROM shop WHERE name = {ToSqlLiteral(shop.Name)})";

    private static string BuildEmployeeRef(EmployeeModel employee)
        => $"(SELECT id FROM employee WHERE first_name = {ToSqlLiteral(employee.FirstName)} AND last_name = {ToSqlLiteral(employee.LastName)})";

    private static string BuildAvailabilityGroupRef(AvailabilityGroupModel group)
        => $"(SELECT id FROM availability_group WHERE year = {group.Year} AND month = {group.Month} AND name = {ToSqlLiteral(group.Name)})";

    private static string BuildAvailabilityGroupMemberRef(AvailabilityGroupModel group, EmployeeModel employee)
        => "(SELECT agm.id FROM availability_group_member agm " +
           "JOIN availability_group ag ON ag.id = agm.availability_group_id " +
           $"WHERE agm.employee_id = {BuildEmployeeRef(employee)} " +
           $"AND ag.year = {group.Year} AND ag.month = {group.Month} AND ag.name = {ToSqlLiteral(group.Name)})";

    private static string BuildScheduleRef(ContainerModel container, ShopModel shop, ScheduleModel graph)
        => $"(SELECT id FROM schedule WHERE {BuildScheduleExistsCondition(container, shop, graph)})";

    private static string BuildScheduleExistsCondition(ContainerModel container, ShopModel shop, ScheduleModel graph)
        => $"container_id = {BuildContainerRef(container)} " +
           $"AND shop_id = {BuildShopRef(shop)} " +
           $"AND name = {ToSqlLiteral(graph.Name)} " +
           $"AND year = {graph.Year} " +
           $"AND month = {graph.Month} " +
           $"AND people_per_shift = {graph.PeoplePerShift} " +
           $"AND shift1_time = {ToSqlLiteral(graph.Shift1Time)} " +
           $"AND shift2_time = {ToSqlLiteral(graph.Shift2Time)}";

    private static string BuildContainerKey(ContainerModel container)
        => container.Name.Trim();

    private static string BuildShopKey(ShopModel shop)
        => shop.Name.Trim();

    private static string BuildEmployeeKey(EmployeeModel employee)
        => $"{employee.FirstName.Trim()}|{employee.LastName.Trim()}";

    private static string BuildAvailabilityGroupKey(AvailabilityGroupModel group)
        => $"{group.Year}|{group.Month}|{group.Name.Trim()}";

    private static string BuildAvailabilityGroupMemberKey(AvailabilityGroupModel group, EmployeeModel employee)
        => $"{BuildAvailabilityGroupKey(group)}|{BuildEmployeeKey(employee)}";

    private static string BuildAvailabilityGroupDayKey(AvailabilityGroupModel group, EmployeeModel employee, AvailabilityGroupDayModel day)
        => $"{BuildAvailabilityGroupMemberKey(group, employee)}|{day.DayOfMonth}";

    private static string BuildScheduleKey(ContainerModel container, ShopModel shop, ScheduleModel graph)
        => $"{BuildContainerKey(container)}|{BuildShopKey(shop)}|{graph.Year}|{graph.Month}|{graph.Name.Trim()}|{graph.PeoplePerShift}|{graph.Shift1Time.Trim()}|{graph.Shift2Time.Trim()}";

    private static string BuildScheduleEmployeeKey(ContainerModel container, ShopModel shop, ScheduleModel graph, EmployeeModel employee)
        => $"{BuildScheduleKey(container, shop, graph)}|{BuildEmployeeKey(employee)}";

    private static string BuildScheduleSlotKey(ContainerModel container, ShopModel shop, ScheduleModel graph, ScheduleSlotModel slot)
        => $"{BuildScheduleKey(container, shop, graph)}|{slot.DayOfMonth}|{slot.SlotNo}|{slot.FromTime.Trim()}|{slot.ToTime.Trim()}";

    private static string BuildScheduleCellStyleKey(ContainerModel container, ShopModel shop, ScheduleModel graph, EmployeeModel employee, ScheduleCellStyleModel style)
        => $"{BuildScheduleKey(container, shop, graph)}|style|{style.DayOfMonth}|{BuildEmployeeKey(employee)}";

    private static string ToSqlLiteral(object? value)
    {
        if (value is null)
        {
            return "NULL";
        }

        if (value is string s)
        {
            return $"'{s.Replace("'", "''")}'";
        }

        if (value is bool b)
        {
            return b ? "1" : "0";
        }

        if (value is DateTime dt)
        {
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        }

        if (value is Enum)
        {
            return $"'{value}'";
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? "NULL";
        }

        return $"'{value.ToString()?.Replace("'", "''")}'";
    }

    private static string EscapeCsv(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

        if (text.IndexOfAny([',', '"', '\n', '\r']) >= 0)
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}
