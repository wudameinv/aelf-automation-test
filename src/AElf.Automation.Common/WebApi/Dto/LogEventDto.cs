namespace AElf.Automation.Common.WebApi.Dto
{
    public class LogEventDto
    {
        public string Address { get; set; }

        public string Name { get; set; }

        public string[] Indexed { get; set; }

        public string NonIndexed { get; set; }
    }
}