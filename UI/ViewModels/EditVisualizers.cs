using Core;
using System;

namespace UI.ViewModels;

public class EditVisualizers
{
    public Model AxisVisualizerX;

    public Model AxisVisualizerY;

    public Model AxisVisualizerZ;

    public static EditVisualizers? _instance = null;
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
                    AxisVisualizerX.Rotation = new OpenTK.Mathematics.Vector3(0, 0, MathF.PI/2);
                    break;
                case SceneHierarchy.YPlaneName:
                    AxisVisualizerY = model;
                    break;
                 case SceneHierarchy.ZPlaneName:
                    AxisVisualizerZ = model;
                    AxisVisualizerZ.Rotation = new OpenTK.Mathematics.Vector3(MathF.PI / 2, 0, 0);
                    break;

            }
        }
        if(AxisVisualizerX is null || AxisVisualizerY is null || AxisVisualizerZ is null)
        {
            throw new Exception("Failed to initialize edit visualizers, not all planes found!");
        }

        GLComponent.OnBoundToModel += OnGlComponentBound;

    }

    public static void SetupInstance() => _instance = new EditVisualizers();

    private void OnGlComponentBound(GLComponent component)
    {
        switch (component.Model.ObjectName)
        {
            case SceneHierarchy.XPlaneName:
                component.color = new OpenTK.Mathematics.Color4(1, 0, 0, 1.0f);
                break;
            case SceneHierarchy.YPlaneName:
                component.color = new OpenTK.Mathematics.Color4(0, 1, 0, 1.0f);
                break;
            case SceneHierarchy.ZPlaneName:
                component.color = new OpenTK.Mathematics.Color4(0, 0, 1, 1.0f);
                break;
            default:
                return;//Do nothing
        }

        component.IsFullbright = true;
        component.UseTilemapRendering = true;
    }
}
