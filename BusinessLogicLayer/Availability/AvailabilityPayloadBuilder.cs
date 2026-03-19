using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Availability
{
    public static class AvailabilityPayloadBuilder
    {
        public static bool TryBuild(
            IEnumerable<(int employeeId, IList<(int day, string code)> codes)> raw,
            out List<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
            out string? error)
        {
            payload = new();
            error = null;

            foreach (var (employeeId, codes) in raw)
            {
                var list = new List<AvailabilityGroupDayModel>(codes.Count);

                foreach (var (day, code) in codes)
                {
                    if (!AvailabilityCodeParser.TryParse(code, out var kind, out var interval))
                    {
                        error = $"Invalid code '{code}' for day {day} (employee #{employeeId}).";
                        return false;
                    }

                    list.Add(new AvailabilityGroupDayModel
                    {
                        Id = 0,
                        AvailabilityGroupMemberId = 0, // заповнює сервіс
                        DayOfMonth = day,
                        Kind = kind,
                        IntervalStr = interval
                    });
                }

                payload.Add((employeeId, list));
            }

            return true;
        }
    }
}
