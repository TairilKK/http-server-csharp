class ArgsHandler{
    string[] args;
    public ArgsHandler(string[] args){
        this.args = args;
    }
    public string? GetDirectory(){
        string? directory = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--directory" && i + 1 < args.Length)
            {
                directory = args[i + 1];
                break;
            }
        }
        return directory;
    }

}
