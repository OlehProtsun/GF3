using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Export;

internal sealed record GraphRelatedScheduleHintSource(
    ScheduleModel Graph,
    IReadOnlyList<ScheduleSlotModel> Slots);

internal static class GraphRelatedScheduleHintExportBuilder
{
    public static IReadOnlyList<GraphNoteExportTextCell> BuildTextCells(
        ScheduleModel currentGraph,
        IReadOnlyList<ScheduleEmployeeModel> currentEmployees,
        IReadOnlyList<ScheduleSlotModel> currentSlots,
        IReadOnlyList<GraphNoteExportTextCell> persistedTextCells,
        IReadOnlyList<GraphRelatedScheduleHintSource> relatedGraphs)
    {
        var employeeIdSet = currentEmployees
            .Where(employee => employee.EmployeeId > 0)
            .Select(employee => employee.EmployeeId)
            .ToHashSet();

        if (employeeIdSet.Count == 0 || relatedGraphs.Count == 0)
            return [];

        var occupiedCells = currentSlots
            .Where(slot => slot.EmployeeId is int employeeId && employeeIdSet.Contains(employeeId))
            .Select(slot => (EmployeeId: slot.EmployeeId!.Value, slot.DayOfMonth))
            .ToHashSet();

        foreach (var textCell in persistedTextCells.Where(textCell => textCell.EmployeeId > 0 && textCell.DayOfMonth > 0))
            occupiedCells.Add((textCell.EmployeeId, textCell.DayOfMonth));

        var graphNamesByCell = new Dictionary<(int EmployeeId, int DayOfMonth), HashSet<string>>();

        foreach (var relatedGraph in relatedGraphs)
        {
            if (relatedGraph.Graph.Id == currentGraph.Id
                || relatedGraph.Graph.Year != currentGraph.Year
                || relatedGraph.Graph.Month != currentGraph.Month)
            {
                continue;
            }

            var graphName = relatedGraph.Graph.Name.Trim();
            if (string.IsNullOrWhiteSpace(graphName))
                continue;

            var seenCellsInGraph = new HashSet<(int EmployeeId, int DayOfMonth)>();

            foreach (var slot in relatedGraph.Slots)
            {
                if (slot.EmployeeId is not int employeeId || !employeeIdSet.Contains(employeeId))
                    continue;

                var cellKey = (EmployeeId: employeeId, slot.DayOfMonth);
                if (occupiedCells.Contains(cellKey) || !seenCellsInGraph.Add(cellKey))
                    continue;

                if (!graphNamesByCell.TryGetValue(cellKey, out var graphNames))
                {
                    graphNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                    graphNamesByCell[cellKey] = graphNames;
                }

                graphNames.Add(graphName);
            }
        }

        return graphNamesByCell
            .OrderBy(entry => entry.Key.EmployeeId)
            .ThenBy(entry => entry.Key.DayOfMonth)
            .Select(entry => new GraphNoteExportTextCell(
                entry.Key.EmployeeId,
                entry.Key.DayOfMonth,
                string.Join(", ", entry.Value.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))))
            .ToList();
    }

    public static IReadOnlyList<GraphNoteExportTextCell> MergeTextCells(
        IReadOnlyList<GraphNoteExportTextCell> persistedTextCells,
        IReadOnlyList<GraphNoteExportTextCell> dynamicTextCells)
    {
        var mergedTextCells = new List<GraphNoteExportTextCell>(persistedTextCells.Count + dynamicTextCells.Count);
        var seenCells = new HashSet<(int EmployeeId, int DayOfMonth)>();

        foreach (var textCell in persistedTextCells.Concat(dynamicTextCells))
        {
            if (textCell.EmployeeId <= 0 || textCell.DayOfMonth <= 0 || string.IsNullOrWhiteSpace(textCell.Value))
                continue;

            var cellKey = (textCell.EmployeeId, textCell.DayOfMonth);
            if (!seenCells.Add(cellKey))
                continue;

            mergedTextCells.Add(textCell);
        }

        return mergedTextCells;
    }
}
