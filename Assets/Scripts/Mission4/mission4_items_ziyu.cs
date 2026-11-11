using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum M4Food { Bun, Lettuce, Tomato, Patty }
public enum M4State { Raw, Cut, Cooked }

[Serializable]
public struct M4Item
{
    public M4Food food;
    public M4State state;

    public M4Item(M4Food food, M4State state)
    {
        this.food = food;
        this.state = state;
    }

    public string DisplayName
    {
        get
        {
            switch (food)
            {
                case M4Food.Bun: return "Burger Bun";
                case M4Food.Lettuce:
                    return state == M4State.Cut ? "Cut Lettuce" : "Lettuce (raw)";
                case M4Food.Tomato:
                    return state == M4State.Cut ? "Cut Tomato" : "Tomato (raw)";
                default:
                    if (state == M4State.Cooked) return "Fried Patty";
                    if (state == M4State.Cut) return "Cut Patty (not fried)";
                    return "Patty (raw)";
            }
        }
    }
}

// 食物外观：全部用基础几何体拼出来
public static class Mission4_Visual_ziyu
{
    public static Color ColorOf(M4Item it)
    {
        switch (it.food)
        {
            case M4Food.Bun: return new Color(0.86f, 0.62f, 0.30f);
            case M4Food.Lettuce:
                return it.state == M4State.Cut
                    ? new Color(0.55f, 0.85f, 0.35f)
                    : new Color(0.25f, 0.70f, 0.20f);
            case M4Food.Tomato:
                return it.state == M4State.Cut
                    ? new Color(0.95f, 0.30f, 0.25f)
                    : new Color(0.85f, 0.10f, 0.08f);
            default:
                if (it.state == M4State.Cooked) return new Color(0.35f, 0.20f, 0.10f);
                if (it.state == M4State.Cut) return new Color(0.90f, 0.50f, 0.50f);
                return new Color(0.80f, 0.35f, 0.35f);
        }
    }

    static void Kill(UnityEngine.Object o)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(o);
        else UnityEngine.Object.DestroyImmediate(o);
    }

    static GameObject Part(Transform parent, PrimitiveType shape, Vector3 localPos,
        Vector3 localScale, Vector3 euler, Color color, Material mat,
        Func<Color, Material> matProvider)
    {
        GameObject g = GameObject.CreatePrimitive(shape);
        Collider c = g.GetComponent<Collider>();
        if (c != null) Kill(c);

        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = localScale;
        g.transform.localEulerAngles = euler;

        Renderer r = g.GetComponent<Renderer>();
        if (matProvider != null)
        {
            r.sharedMaterial = matProvider(color);
        }
        else
        {
            if (mat != null) r.sharedMaterial = mat;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }
        return g;
    }

    // matProvider 只在编辑器搭建场景时使用（为每种颜色创建材质资源）
    public static GameObject Make(M4Item it, Material mat, Func<Color, Material> matProvider = null)
    {
        GameObject root = new GameObject("Food_" + it.food + "_" + it.state);
        Color c = ColorOf(it);
        Transform t = root.transform;

        switch (it.food)
        {
            case M4Food.Bun:
                Part(t, PrimitiveType.Sphere, new Vector3(0, 0.07f, 0),
                    new Vector3(0.45f, 0.26f, 0.45f), Vector3.zero, c, mat, matProvider);
                Part(t, PrimitiveType.Cylinder, new Vector3(0, -0.04f, 0),
                    new Vector3(0.40f, 0.03f, 0.40f), Vector3.zero,
                    c * 0.8f, mat, matProvider);
                break;

            case M4Food.Lettuce:
                if (it.state == M4State.Cut)
                {
                    for (int i = 0; i < 3; i++)
                        Part(t, PrimitiveType.Cube, new Vector3(0, 0.03f + i * 0.045f, 0),
                            new Vector3(0.36f, 0.04f, 0.30f), new Vector3(0, i * 35f, 0),
                            c, mat, matProvider);
                }
                else
                {
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 0.18f, 0),
                        new Vector3(0.38f, 0.36f, 0.38f), Vector3.zero, c, mat, matProvider);
                }
                break;

            case M4Food.Tomato:
                if (it.state == M4State.Cut)
                {
                    for (int i = 0; i < 3; i++)
                        Part(t, PrimitiveType.Cylinder,
                            new Vector3((i - 1) * 0.12f, 0.02f + i * 0.015f, 0),
                            new Vector3(0.24f, 0.012f, 0.24f), Vector3.zero,
                            c, mat, matProvider);
                }
                else
                {
                    Part(t, PrimitiveType.Sphere, new Vector3(0, 0.15f, 0),
                        new Vector3(0.30f, 0.28f, 0.30f), Vector3.zero, c, mat, matProvider);
                }
                break;

            default:
                if (it.state == M4State.Raw)
                {
                    Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.04f, 0),
                        new Vector3(0.42f, 0.04f, 0.42f), Vector3.zero, c, mat, matProvider);
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f * Mathf.Deg2Rad;
                        Part(t, PrimitiveType.Cylinder,
                            new Vector3(Mathf.Cos(a) * 0.1f, 0.03f, Mathf.Sin(a) * 0.1f),
                            new Vector3(0.18f, 0.03f, 0.18f), Vector3.zero,
                            c, mat, matProvider);
                    }
                }
                break;
        }

        return root;
    }
}
