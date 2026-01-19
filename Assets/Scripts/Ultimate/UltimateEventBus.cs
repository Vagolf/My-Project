using System;
using UnityEngine;

public static class UltimateEventBus
{
    public static event Action<Transform> OnUltimateStart;
    public static event Action<Transform> OnUltimateDamage;
    public static event Action<Transform> OnUltimateFinish;

    public static void RaiseStart(Transform t) => OnUltimateStart?.Invoke(t);
    public static void RaiseDamage(Transform t) => OnUltimateDamage?.Invoke(t);
    public static void RaiseFinish(Transform t) => OnUltimateFinish?.Invoke(t);
}
