public class SystemRandomPicker: IRandomPicker
{
    private System.Random random = new System.Random();

    public int Pick(int count)
    {
        return random.Next(count);
    }
}
