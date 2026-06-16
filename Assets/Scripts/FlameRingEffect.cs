using UnityEngine;

public sealed class FlameRingEffect : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            return;
        }

        GameObject effectObject = new("MobileFlameRing");
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = Vector3.zero;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.25f;
        main.startSize = 0.06f;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 35f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 0.35f;
        shape.donutRadius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.55f, 0.08f), 0f),
                new GradientColorKey(new Color(1f, 0.12f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;
    }
}
