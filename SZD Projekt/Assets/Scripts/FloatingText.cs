using TMPro;
using UnityEngine;

/// <summary>A short-lived label that rises and fades out, then destroys itself. Created by <see cref="GridManager.ShowHpChange"/>.</summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    private const float Lifetime = 0.8f;
    private const float RiseDistance = 0.6f;

    private TextMeshPro _label;
    private float _age;

    private void Awake() => _label = GetComponent<TextMeshPro>();

    private void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (RiseDistance / Lifetime * Time.deltaTime);
        _label.alpha = 1f - _age / Lifetime;
        if (_age >= Lifetime) Destroy(gameObject);
    }
}
