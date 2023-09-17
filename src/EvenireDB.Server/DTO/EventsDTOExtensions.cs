public static class EventsDTOExtensions
{
    public static Event[] ToModels(this EventDTO[] dtos)
    {
        int count = dtos.Length;
        var results = new Event[count];          
        for(int  i = 0; i < count; i++)
        {
            results[i] = dtos[i].ToModel();
        }
        return results;
    }
}