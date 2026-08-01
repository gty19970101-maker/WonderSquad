namespace WonderSquad.Content.Validation
{
    public readonly struct ContentValidationIssue
    {
        public ContentValidationIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }

        public string Message { get; }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}

