using Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UI.ViewModels;

public class EditVisualizers
{
    public Model AxisVisualizerX { get; set; }
    public Model AxisVisualizerY { get; set; }
    public Model AxisVisualizerZ { get; set; }
    public Model OriginVisualizerX { get; set; }
    public Model OriginVisualizerY { get; set; }
    public Model OriginVisualizerZ { get; set; }

    private static EditVisualizers? _instance;
    public static EditVisualizers Instance { 
        get 
        { 
            if(_instance == null) throw new InvalidOperationException($"{nameof(EditVisualizers)} is being called while not initialized!");
            return _instance; 
        } 
        private set 
        { 
            _instance = value; 
        } 
    }

    public EditVisualizers()
    {
        foreach (Model model in SceneHierarchy.Instance.GetModels(HierarchyType.EditVisualizer))
        {
            switch(model.ObjectName)
            {
                case SceneHierarchy.XPlaneName:
                    AxisVisualizerX = model;
                    AxisVisualizerX.Rotation = new OpenTK.Mathematics.Vector3(MathF.PI / 2, 0, 0);
                    break;
                case SceneHierarchy.YPlaneName:
                    AxisVisualizerY = model;
                    break;
                 case SceneHierarchy.ZPlaneName:
                    AxisVisualizerZ = model;
                    AxisVisualizerZ.Rotation = new OpenTK.Mathematics.Vector3(0, 0, MathF.PI / 2);
                    break;

            }
        }
        if(AxisVisualizerX is null || AxisVisualizerY is null || AxisVisualizerZ is null)
        {
            throw new InvalidOperationException("Failed to initialize edit visualizers, not all planes found!");
        }

        foreach (Model model in SceneHierarchy.Instance.GetModels(HierarchyType.Tool))
        {
            switch (model.ObjectName)
            {
                case SceneHierarchy.XAxisName:
                    OriginVisualizerX = model;
                    break;
                case SceneHierarchy.YAxisName:
                    OriginVisualizerY = model;
                    break;
                case SceneHierarchy.ZAxisName:
                    OriginVisualizerZ = model;
                    break;

            }
        }

        GLComponent.OnBoundToModel += OnGlComponentBound;

    }

    public static void SetupInstance() => _instance = new EditVisualizers();

    private void OnGlComponentBound(GLComponent component)
    {
        switch (component.Model.ObjectName)
        {
            case SceneHierarchy.XAxisName:
            case SceneHierarchy.XPlaneName:
                component.Color = new OpenTK.Mathematics.Color4(1, 0, 0, 1.0f);
                break;
            case SceneHierarchy.YAxisName:
            case SceneHierarchy.YPlaneName:
                component.Color = new OpenTK.Mathematics.Color4(0, 1, 0, 1.0f);
                break;
            case SceneHierarchy.ZAxisName:
            case SceneHierarchy.ZPlaneName:
                component.Color = new OpenTK.Mathematics.Color4(0, 0, 1, 1.0f);
                break;
            default:
                return;//Do nothing
        }

        component.IsFullbright = true;
        component.UseTilemapRendering = true;

        if (component.Model.ObjectName.Equals(SceneHierarchy.ZPlaneName, StringComparison.Ordinal)
            || component.Model.ObjectName.Equals(SceneHierarchy.XPlaneName, StringComparison.Ordinal)
            || component.Model.ObjectName.Equals(SceneHierarchy.YPlaneName, StringComparison.Ordinal))
        {
            component.Hidden = true;//Start hidden, only show when needed.
        }
    }

    public IEnumerable<Model> AllVisualizers
    {
        get
        {
            yield return AxisVisualizerX;
            yield return AxisVisualizerY;
            yield return AxisVisualizerZ;
        }
    }
}
