using UnityEngine;

namespace MegaTanks
{
    public static class Ballistics2D
    {
        // Find the earliest intercept at a fixed launch speed, choosing the lower arc.
        public static bool TryIntercept(Vector2 origin, Vector2 target, Vector2 targetVelocity,
            float speed, float gravity, out Vector2 launchVelocity)
        {
            launchVelocity = Vector2.zero;
            if (speed <= 0) return false;
            Vector2 delta = target - origin;
            if (gravity < .0001f)
            {
                float a = targetVelocity.sqrMagnitude - speed * speed;
                float b = 2 * Vector2.Dot(delta, targetVelocity);
                float c = delta.sqrMagnitude;
                float time;
                if (Mathf.Abs(a) < .0001f)
                {
                    if (b >= 0) return false;
                    time = -c / b;
                }
                else
                {
                    float discriminant = b * b - 4 * a * c;
                    if (discriminant < 0) return false;
                    float first = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                    float second = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                    time = first > 0 && second > 0 ? Mathf.Min(first, second) : Mathf.Max(first, second);
                }
                if (time <= 0) return false;
                launchVelocity = (delta + targetVelocity * time) / time;
                return true;
            }

            float horizon = 2 * speed / gravity + 2 * delta.magnitude / Mathf.Max(1, speed - targetVelocity.magnitude);
            float previous = .00001f;
            for (int i = 1; i <= 256; i++)
            {
                float time = Mathf.Max(.00002f, horizon * i / 256);
                Vector2 required = delta + targetVelocity * time + Vector2.up * (.5f * gravity * time * time);
                if (required.sqrMagnitude <= speed * speed * time * time)
                {
                    float low = previous, high = time;
                    for (int step = 0; step < 24; step++)
                    {
                        float middle = (low + high) * .5f;
                        Vector2 offset = delta + targetVelocity * middle + Vector2.up * (.5f * gravity * middle * middle);
                        if (offset.sqrMagnitude > speed * speed * middle * middle) low = middle;
                        else high = middle;
                    }
                    float flightTime = (low + high) * .5f;
                    launchVelocity = (delta + targetVelocity * flightTime + Vector2.up * (.5f * gravity * flightTime * flightTime)) / flightTime;
                    return true;
                }
                previous = time;
            }
            return false;
        }
    }
}
