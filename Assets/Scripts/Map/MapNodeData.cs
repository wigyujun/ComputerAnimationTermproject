using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapNodeData
{
    public string nodeId;
    public int floorIndex;
    public int columnIndex;
    public int laneIndex;

    public NodeType nodeType;
    public ThemeType themeType;
    public NodeState nodeState;

    public Vector2 uiPosition;
    public List<string> nextNodeIds = new List<string>();
}
