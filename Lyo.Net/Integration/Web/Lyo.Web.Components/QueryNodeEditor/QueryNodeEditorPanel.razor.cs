using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.Web.Components.QueryNodeEditor;

public partial class QueryNodeEditorPanel
{
    private WhereClause? _draggedNode;

    private WhereClause? _dragOverNode;

    private QueryNodeEditor? _nodeEditorRef;

    private WhereClause? _rootNode;

    private WhereClause? _selectedNode;

    private IReadOnlyCollection<TreeItemData<WhereClauseTreeItem>> _treeItems = new List<TreeItemData<WhereClauseTreeItem>>();

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public WhereClause? WhereClause { get; set; }

    [Parameter]
    public IEnumerable<FilterPropertyDefinition> PropertyDefinitions { get; set; } = [];

    [Parameter]
    public EventCallback<WhereClause?> WhereClauseChanged { get; set; }

    [Parameter]
    public EventCallback<WhereClause?> SelectedNodeChanged { get; set; }

    [Parameter]
    public bool AllowDragDrop { get; set; } = true;

    [Parameter]
    public bool AutoSelectNewNode { get; set; } = true;

    protected override void OnInitialized()
    {
        if (WhereClause != null) {
            _rootNode = WhereClause;
            SetSelectedNode(_rootNode);
        }

        RebuildTree();
    }

    protected override void OnParametersSet()
    {
        // Update root node when WhereClause parameter changes
        if (WhereClause != _rootNode) {
            if (WhereClause != null) {
                _rootNode = WhereClause;
                SetSelectedNode(_rootNode);
            }
            else {
                _rootNode = null;
                SetSelectedNode(null);
            }

            RebuildTree();
        }
    }

    private void RebuildTree()
    {
        if (_rootNode == null) {
            _treeItems = new List<TreeItemData<WhereClauseTreeItem>>();
            return;
        }

        var rootItem = BuildTreeItem(_rootNode);
        _treeItems = new List<TreeItemData<WhereClauseTreeItem>> { rootItem };
    }

    private TreeItemData<WhereClauseTreeItem> BuildTreeItem(WhereClause node)
    {
        var item = new WhereClauseTreeItem { Node = node };
        List<TreeItemData<WhereClauseTreeItem>> children = [];
        if (node is GroupClause groupClause) {
            if (groupClause.Children != null && groupClause.Children.Any())
                children.AddRange(groupClause.Children.Select(BuildTreeItem));

            if (groupClause.SubClause != null)
                children.Add(BuildTreeItem(groupClause.SubClause));
        }
        else if (node is ConditionClause conditionClause) {
            if (conditionClause.SubClause != null)
                children.Add(BuildTreeItem(conditionClause.SubClause));
        }

        return new() {
            Value = item,
            Expanded = true,
            Children = children.Count > 0 ? children : null,
            Expandable = children.Count > 0
        };
    }

    private void PrepareDrag(WhereClause node)
    {
        if (!AllowDragDrop)
            return;

        _draggedNode = node;
        StateHasChanged();
    }

    private void OnDragEnd()
    {
        if (!AllowDragDrop)
            return;

        _draggedNode = null;
        _dragOverNode = null;
        StateHasChanged();
    }

    private EventCallback<DragEventArgs> DragOverCallback(WhereClause targetNode) => EventCallback.Factory.Create<DragEventArgs>(this, e => OnDragOver(e, targetNode));

    private EventCallback<DragEventArgs> DropCallback(WhereClause targetNode) => EventCallback.Factory.Create<DragEventArgs>(this, e => OnDrop(e, targetNode));

    private void OnDragOver(DragEventArgs e, WhereClause targetNode)
    {
        if (!AllowDragDrop)
            return;

        if (_draggedNode == null)
            return;

        // Only allow dropping on group clauses
        // Allow dropping into parent nodes, but prevent dropping into self or creating cycles
        if (targetNode is not GroupClause || targetNode == _draggedNode || IsDescendantOf(targetNode, _draggedNode)) // Prevent dropping into own descendants (cycle prevention)
        {
            // Clear drag over if this was the current target
            if (_dragOverNode == targetNode) {
                _dragOverNode = null;
                StateHasChanged();
            }

            return;
        }

        // If we already have a drag over node, prefer the more nested one (descendant)
        if (_dragOverNode != null) {
            if (IsDescendantOf(_dragOverNode, targetNode)) {
                // Current target is more nested, keep it
                return;
            }

            if (IsDescendantOf(targetNode, _dragOverNode)) {
                // New target is more nested, use it
                _dragOverNode = targetNode;
                StateHasChanged();
                return;
            }
        }

        // Set new drag over target
        if (_dragOverNode != targetNode) {
            _dragOverNode = targetNode;
            StateHasChanged();
        }
    }

    private void OnDragLeave()
    {
        if (!AllowDragDrop)
            return;

        _dragOverNode = null;
        StateHasChanged();
    }

    private void OnDrop(DragEventArgs e, WhereClause targetNode)
    {
        if (!AllowDragDrop)
            return;

        if (_draggedNode == null) {
            _dragOverNode = null;
            return;
        }

        // Allow dropping into group clauses only (not ConditionClause.SubClause — SubQuery is edited via UI)
        if (targetNode is not GroupClause || targetNode == _draggedNode || IsDescendantOf(targetNode, _draggedNode)) {
            _dragOverNode = null;
            _draggedNode = null;
            return;
        }

        var nodeToMove = _draggedNode;

        // Remove node from its current parent first
        if (nodeToMove == _rootNode) {
            // Can't move root node - skip
            _dragOverNode = null;
            _draggedNode = null;
            return;
        }

        // Find and remove from current parent
        var removed = RemoveNodeFromParent(_rootNode, nodeToMove);
        if (!removed) {
            _dragOverNode = null;
            _draggedNode = null;
            return;
        }

        // Add to target group clause
        if (targetNode is GroupClause targetGroupClause) {
            targetGroupClause.Children ??= [];
            if (!targetGroupClause.Children.Contains(nodeToMove))
                targetGroupClause.Children.Add(nodeToMove);
        }

        _dragOverNode = null;
        _draggedNode = null;
        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private bool IsDescendantOf(WhereClause potentialDescendant, WhereClause ancestor)
    {
        if (ancestor is GroupClause logicalAncestor && logicalAncestor.Children != null) {
            if (logicalAncestor.Children.Contains(potentialDescendant))
                return true;

            foreach (var child in logicalAncestor.Children) {
                if (IsDescendantOf(potentialDescendant, child))
                    return true;
            }
        }

        if (ancestor is GroupClause logicalWithSub && logicalWithSub.SubClause != null) {
            if (logicalWithSub.SubClause == potentialDescendant)
                return true;

            if (IsDescendantOf(potentialDescendant, logicalWithSub.SubClause))
                return true;
        }

        if (ancestor is ConditionClause conditionAncestor && conditionAncestor.SubClause != null) {
            if (conditionAncestor.SubClause == potentialDescendant)
                return true;

            if (IsDescendantOf(potentialDescendant, conditionAncestor.SubClause))
                return true;
        }

        return false;
    }

    private string GetNodeDisplayText(WhereClause node)
    {
        if (node is GroupClause groupClause) {
            var childCount = groupClause.Children?.Count ?? 0;
            var desc = !string.IsNullOrWhiteSpace(groupClause.Description) ? $" - {groupClause.Description}" : "";
            var subQueryHint = groupClause.SubClause != null ? " [+SubQuery]" : "";
            return $"{groupClause.Operator} ({childCount} condition{(childCount != 1 ? "s" : "")}){desc}{subQueryHint}";
        }

        if (node is ConditionClause conditionClause) {
            var valueStr = conditionClause.Value != null ? $" {conditionClause.Value}" : conditionClause.Value == null ? " null" : "";
            var desc = !string.IsNullOrWhiteSpace(conditionClause.Description) ? $" - {conditionClause.Description}" : "";
            var subQueryHint = conditionClause.SubClause != null ? " [+SubQuery]" : "";
            return $"{conditionClause.Field} {conditionClause.Comparison}{valueStr}{desc}{subQueryHint}";
        }

        return node.ToString() ?? "Unknown";
    }

    private string GetNodeIcon(WhereClause node) => node is GroupClause ? Icons.Material.Filled.AccountTree : Icons.Material.Filled.FilterList;

    private Color GetNodeIconColor(WhereClause node)
    {
        if (node is GroupClause groupClause)
            return groupClause.Operator == GroupOperatorEnum.And ? Color.Primary : Color.Secondary;

        return Color.Success;
    }

    private string GetDragCursor() => AllowDragDrop ? "move" : "default";

    private void SelectNode(WhereClause node)
    {
        SetSelectedNode(node);
        StateHasChanged();
    }

    private void HandleNodeChanged(WhereClause node)
    {
        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private void NotifyQueryChanged() => WhereClauseChanged.InvokeAsync(_rootNode);

    private bool ValidateSelectedNode()
    {
        if (_nodeEditorRef == null)
            return true;

        var isValid = _nodeEditorRef.ValidateCurrentNode();
        if (!isValid)
            Snackbar.Add("Please fix validation errors on the current node before adding new nodes.", Severity.Warning);

        return isValid;
    }

    private void AddGroupClause()
    {
        if (_rootNode == null) {
            _rootNode = new GroupClause(GroupOperatorEnum.And);
            SelectNewNode(_rootNode);
        }
        else if (_rootNode is ConditionClause) {
            //todo this should never hit
            var oldRoot = _rootNode;
            _rootNode = WhereClauseBuilder.And(and => {
                and.Add(oldRoot);
            });

            SelectNewNode(_rootNode);
        }
        else if (_rootNode is GroupClause rootLogical) {
            rootLogical.Children ??= [];
            var newNode = new GroupClause(GroupOperatorEnum.And);
            rootLogical.Children.Add(newNode);
            SelectNewNode(newNode);
        }

        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private void AddGroupClauseAtSelected()
    {
        if (!ValidateSelectedNode())
            return;

        if (_selectedNode == null) {
            AddGroupClause();
            return;
        }

        if (_selectedNode is GroupClause selectedLogical) {
            // Add as child of selected group clause
            selectedLogical.Children ??= [];
            var newNode = new GroupClause(GroupOperatorEnum.And);
            selectedLogical.Children.Add(newNode);
            SelectNewNode(newNode);
        }
        else {
            // Selected node is a condition clause — wrap it in a group clause
            WrapNodeInLogical(_selectedNode);
        }

        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private void AddSubClauseAtSelected()
    {
        if (!ValidateSelectedNode())
            return;

        if (_selectedNode is ConditionClause cn && cn.SubClause == null) {
            cn.SubClause = WhereClauseBuilder.And(and => and.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null)));
            RebuildTree();
            NotifyQueryChanged();
            SelectNewNode(cn.SubClause);
            StateHasChanged();
        }
        else if (_selectedNode is GroupClause ln && ln.SubClause == null) {
            ln.SubClause = WhereClauseBuilder.And(and => and.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null)));
            RebuildTree();
            NotifyQueryChanged();
            SelectNewNode(ln.SubClause);
            StateHasChanged();
        }
    }

    private void AddConditionClauseAtSelected()
    {
        if (!ValidateSelectedNode())
            return;

        if (_selectedNode == null) {
            AddConditionClause();
            return;
        }

        if (_selectedNode is GroupClause selectedLogical) {
            // Add as child of selected group clause
            selectedLogical.Children ??= [];
            var newNode = WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null);
            selectedLogical.Children.Add(newNode);
            SelectNewNode(newNode);
        }
        else {
            // Selected node is a condition clause — wrap it in a group clause
            WrapNodeInLogical(_selectedNode);
            // Now add the new condition as a sibling
            if (_selectedNode is GroupClause wrappedLogical) {
                wrappedLogical.Children ??= [];
                var newNode = WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null);
                wrappedLogical.Children.Add(newNode);
                SelectNewNode(newNode);
            }
        }

        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private void WrapNodeInLogical(WhereClause nodeToWrap)
    {
        if (nodeToWrap == _rootNode) {
            // Wrap root node
            var oldRoot = _rootNode;
            _rootNode = WhereClauseBuilder.And(and => {
                and.Add(oldRoot);
            });

            SetSelectedNode(_rootNode);
        }
        else {
            // Find parent and replace the node with a group-clause wrapper
            ReplaceNodeWithLogicalWrapper(_rootNode, nodeToWrap);
        }
    }

    private bool ReplaceNodeWithLogicalWrapper(WhereClause? parent, WhereClause nodeToReplace)
    {
        if (parent is not GroupClause groupClauseParent)
            return false;

        if (groupClauseParent.Children != null) {
            var index = groupClauseParent.Children.IndexOf(nodeToReplace);
            if (index >= 0) {
                // Replace the node with a group-clause wrapper
                var logicalWrapper = WhereClauseBuilder.And(and => {
                    and.Add(nodeToReplace);
                });

                groupClauseParent.Children[index] = logicalWrapper;
                SetSelectedNode(logicalWrapper);
                return true;
            }

            // Recursively search children
            foreach (var child in groupClauseParent.Children) {
                if (ReplaceNodeWithLogicalWrapper(child, nodeToReplace))
                    return true;
            }
        }

        return false;
    }

    private void AddConditionClause()
    {
        if (_rootNode == null) {
            _rootNode = WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null);
            SelectNewNode(_rootNode);
        }
        else if (_rootNode is ConditionClause) {
            var oldRoot = _rootNode;
            _rootNode = WhereClauseBuilder.And(and => {
                and.Add(oldRoot);
                and.Add(WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null));
            });

            SelectNewNode(_rootNode);
        }
        else if (_rootNode is GroupClause rootLogical) {
            rootLogical.Children ??= [];
            var newNode = WhereClauseBuilder.Condition("", ComparisonOperatorEnum.Equals, null);
            rootLogical.Children.Add(newNode);
            SelectNewNode(newNode);
        }

        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private void RemoveNode(WhereClause nodeToRemove)
    {
        if (nodeToRemove == _rootNode) {
            _rootNode = null;
            SetSelectedNode(null);
        }
        else {
            RemoveNodeFromParent(_rootNode, nodeToRemove);
            if (_selectedNode == nodeToRemove)
                SetSelectedNode(_rootNode);
        }

        RebuildTree();
        NotifyQueryChanged();
        StateHasChanged();
    }

    private bool RemoveNodeFromParent(WhereClause? parent, WhereClause nodeToRemove)
    {
        if (parent is ConditionClause conditionClause && conditionClause.SubClause == nodeToRemove) {
            conditionClause.SubClause = null;
            return true;
        }

        if (parent is GroupClause groupClauseWithSub && groupClauseWithSub.SubClause == nodeToRemove) {
            groupClauseWithSub.SubClause = null;
            return true;
        }

        if (parent is GroupClause groupClauseParent && groupClauseParent.Children != null) {
            if (groupClauseParent.Children.Contains(nodeToRemove)) {
                groupClauseParent.Children.Remove(nodeToRemove);
                if (!groupClauseParent.Children.Any())
                    groupClauseParent.Children = [];

                return true;
            }

            foreach (var child in groupClauseParent.Children) {
                if (RemoveNodeFromParent(child, nodeToRemove))
                    return true;
            }

            if (groupClauseParent.SubClause != null) {
                if (RemoveNodeFromParent(groupClauseParent.SubClause, nodeToRemove))
                    return true;
            }
        }

        if (parent is ConditionClause cond && cond.SubClause != null) {
            if (RemoveNodeFromParent(cond.SubClause, nodeToRemove))
                return true;
        }

        return false;
    }

    private void SelectNewNode(WhereClause? node)
    {
        if (AutoSelectNewNode)
            SetSelectedNode(node);
    }

    private void SetSelectedNode(WhereClause? node)
    {
        if (ReferenceEquals(_selectedNode, node))
            return;

        _selectedNode = node;
        if (SelectedNodeChanged.HasDelegate)
            _ = SelectedNodeChanged.InvokeAsync(_selectedNode);
    }

    private static bool CanAddSubClause(WhereClause? node)
        => node switch {
            ConditionClause c => c.SubClause == null,
            GroupClause l => l.SubClause == null,
            var _ => false
        };

    private class WhereClauseTreeItem
    {
        public WhereClause Node { get; set; } = null!;
    }
}