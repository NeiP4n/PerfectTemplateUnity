namespace Sources.Controllers
{
    public class SineMotion
    {
        public float GetSine(float time, float frequency)
        {
            return UnityEngine.Mathf.Sin(time * frequency);
        }
    }
}
