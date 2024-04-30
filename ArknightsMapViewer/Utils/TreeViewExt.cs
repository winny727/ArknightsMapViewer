using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

///<summary>
/// 导航树控件
///</summary>
[DesignTimeVisible(true)]
[Serializable]
public class TreeViewExt : TreeView
{
    #region 成员变量

    ///<summary>
    /// 存储多选时选择的节点
    ///</summary>
    private List<TreeNode> selectedNodeList = new List<TreeNode>();

    ///<summary>
    /// 当前节点
    ///</summary>
    private TreeNode currentNode = null;

    #endregion

    #region 属性

    ///<summary>
    /// 是否是多选
    ///</summary>
    public bool IsMultiSelect { get; set; }

    ///<summary>   
    /// 选择的节点的集合（自动按TreeNode的Index排序）
    ///</summary>
    public List<TreeNode> SelectedNodeList
    {
        get
        {
            //清理已经被移除的Node
            for (int i = selectedNodeList.Count - 1; i >= 0; i--)
            {
                if (selectedNodeList[i] == null || selectedNodeList[i].TreeView == null)
                {
                    selectedNodeList.RemoveAt(i);
                }
            }

            selectedNodeList.Sort((x, y) => x.Index.CompareTo(y.Index));
            return selectedNodeList;
        }
    }

    #endregion

    #region 类函数

    ///<summary>
    /// 构造函数
    ///</summary>
    public TreeViewExt() : base()
    {
        this.DrawMode = TreeViewDrawMode.OwnerDrawText;
    }

    /// <summary>
    /// 触发重绘
    /// </summary>
    public void DrawNodesView()
    {
        BeginUpdate();
        using (Graphics graphics = this.CreateGraphics())
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                TreeNode node = Nodes[i];
                DrawTreeNodeEventArgs arg = new DrawTreeNodeEventArgs(graphics, node, node.Bounds, TreeNodeStates.Default);
                arg.DrawDefault = false;
                OnDrawNode(arg);
            }
        }
        EndUpdate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        DrawNodesView();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        DrawNodesView();
    }

    ///<summary>
    /// 鼠标单击事件
    ///</summary>
    ///<param name="e"></param>
    protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e)
    {
        this.SelectedNode = e.Node;

        // 如果是多选，则根据按钮情况设置节点的选择状态
        if (IsMultiSelect)
        {
            if (e.Button == MouseButtons.Left)
            {
                if ((Control.ModifierKeys & Keys.Control) != 0)
                {
                    CtrlMultiSelectNodes(SelectedNode);
                }
                else if ((Control.ModifierKeys & Keys.Shift) != 0)
                {
                    ShiftMultiSelectNodes(SelectedNode);
                }
                else
                {
                    SingleSelectNode(SelectedNode);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (!SelectedNodeList.Contains(SelectedNode))
                {
                    SingleSelectNode(SelectedNode);
                }
            }
        }
        else
        {
            SingleSelectNode(SelectedNode);
        }

        base.OnNodeMouseClick(e);

        this.Invalidate();
    }


    ///<summary>
    /// 重绘
    ///</summary>
    ///<param name="e"></param>
    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node == null || e.Node.TreeView == null)
        {
            return;
        }

        if (e.Bounds.X == -1)
        {
            return;
        }

        e.DrawDefault = false;

        Font font = this.Font;
        if (e.Node.NodeFont != null) font = e.Node.NodeFont;

        Color foreColor = this.ForeColor;
        if (SelectedNodeList.Contains(e.Node) && this.Focused)
        {
            foreColor = SystemColors.HighlightText;
        }
        else if (e.Node.ForeColor != Color.Empty)
        {
            foreColor = e.Node.ForeColor;
        }

        Graphics graphics = e.Graphics;
        Rectangle bounds = e.Node.Bounds;
        Size size = TextRenderer.MeasureText(e.Node.Text, e.Node.TreeView.Font);
        Point location = new Point(bounds.X + 1, bounds.Y);
        bounds = new Rectangle(location, new Size(size.Width, bounds.Height));

        // 绘制节点的文本
        if (SelectedNodeList.Contains(e.Node) && this.Focused)
        {
            graphics.FillRectangle(SystemBrushes.Highlight, bounds);
            ControlPaint.DrawFocusRectangle(graphics, bounds, foreColor, SystemColors.Highlight);
            TextRenderer.DrawText(graphics, e.Node.Text, font, bounds, foreColor, TextFormatFlags.Default);
        }
        else
        {
            using (Brush brush = new SolidBrush(BackColor))
            {
                graphics.FillRectangle(brush, bounds);
            }
            TextRenderer.DrawText(graphics, e.Node.Text, font, bounds, foreColor, TextFormatFlags.Default);
        }
    }

    #endregion

    #region Private Methods

    ///<summary>   
    /// 按ctrl键多选
    ///</summary>   
    ///<param name="node"></param>   
    private void CtrlMultiSelectNodes(TreeNode node)
    {
        if (node == null)
        {
            return;
        }

        if (SelectedNodeList.Contains(node))
        {
            SelectedNodeList.Remove(node);
        }
        else
        {
            SelectedNodeList.Add(node);
        }
        SetCurrentNode(node);
    }

    ///<summary>
    /// 按shift键多选
    ///</summary>
    ///<param name="node"></param>
    private void ShiftMultiSelectNodes(TreeNode node)
    {
        if (node == null)
        {
            return;
        }

        if (currentNode != null && node.Parent == currentNode.Parent)
        {
            TreeNode addNode = node;
            SelectedNodeList.Clear();
            for (int i = System.Math.Abs(currentNode.Index - node.Index); i >= 0; i--)
            {
                SelectedNodeList.Add(addNode);
                addNode = currentNode.Index > node.Index ? addNode.NextNode : addNode.PrevNode;
            }

            if (!SelectedNodeList.Contains(currentNode))
            {
                SetCurrentNode(node);
            }
        }
        else
        {
            SingleSelectNode(SelectedNode);
        }
    }

    ///<summary>   
    /// Set current node   
    ///</summary>   
    ///<param name="node"></param>
    private void SetCurrentNode(TreeNode node)
    {
        currentNode = node;
    }

    #endregion

    ///<summary>   
    /// Single select   
    ///</summary>   
    ///<param name="node"></param>
    public void SingleSelectNode(TreeNode node)
    {
        SelectedNode = node;
        SelectedNodeList.Clear();
        if (node != null)
        {
            SelectedNodeList.Add(node);
        }
        SetCurrentNode(node);
        DrawNodesView();
    }
}