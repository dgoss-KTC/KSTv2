import { useEffect, useRef, useState } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import './WorkspaceTabBar.css';

interface WorkspaceTabBarProps {
  workspaces: WorkspaceAssignmentDto[];
  activeId: string | null;
  onSelect: (id: string) => void;
  onAdd: (triggerEl: HTMLElement) => void;
  onManage: (triggerEl: HTMLElement) => void;
  onEdit: (workspace: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => void;
  onArchive: (workspace: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => void;
  onDelete: (workspace: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => void;
  onReorder: (orderedIds: string[]) => void;
  isGeneralActive: boolean;
  onSelectGeneral: () => void;
}

export function WorkspaceTabBar({
  workspaces,
  activeId,
  onSelect,
  onAdd,
  onManage,
  onEdit,
  onArchive,
  onDelete,
  onReorder,
  isGeneralActive,
  onSelectGeneral,
}: WorkspaceTabBarProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [dragOverId, setDragOverId] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  // Kebab (workspace-actions) trigger buttons are stable across a menu open/close cycle, so they
  // are the correct focus-restoration target for dialogs opened from a menu item (which unmounts
  // as soon as it's clicked).
  const kebabRefs = useRef<Map<string, HTMLButtonElement>>(new Map());

  useEffect(() => {
    if (!openMenuId) return;
    const handlePointerDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpenMenuId(null);
      }
    };
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpenMenuId(null);
    };
    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [openMenuId]);

  const moveTab = (assignmentId: string, delta: number) => {
    const index = workspaces.findIndex((w) => w.assignmentId === assignmentId);
    const targetIndex = index + delta;
    if (index === -1 || targetIndex < 0 || targetIndex >= workspaces.length) return;

    const reordered = [...workspaces];
    const [moved] = reordered.splice(index, 1);
    reordered.splice(targetIndex, 0, moved);
    onReorder(reordered.map((w) => w.assignmentId));
  };

  const handleDrop = (targetId: string) => {
    setDragOverId(null);
    if (!dragId || dragId === targetId) {
      setDragId(null);
      return;
    }

    const fromIndex = workspaces.findIndex((w) => w.assignmentId === dragId);
    const toIndex = workspaces.findIndex((w) => w.assignmentId === targetId);
    if (fromIndex === -1 || toIndex === -1) {
      setDragId(null);
      return;
    }

    const reordered = [...workspaces];
    const [moved] = reordered.splice(fromIndex, 1);
    reordered.splice(toIndex, 0, moved);
    setDragId(null);
    onReorder(reordered.map((w) => w.assignmentId));
  };

  return (
    <div className="tab-bar" role="tablist" aria-label="Workspaces" ref={containerRef}>
      {workspaces.map((w, index) => (
        <div
          key={w.assignmentId}
          className={`tab-bar__tab-wrapper${dragOverId === w.assignmentId ? ' tab-bar__tab-wrapper--drag-over' : ''}`}
          draggable
          onDragStart={() => setDragId(w.assignmentId)}
          onDragOver={(e) => {
            e.preventDefault();
            if (dragOverId !== w.assignmentId) setDragOverId(w.assignmentId);
          }}
          onDragLeave={() => setDragOverId((prev) => (prev === w.assignmentId ? null : prev))}
          onDrop={(e) => {
            e.preventDefault();
            handleDrop(w.assignmentId);
          }}
          onDragEnd={() => {
            setDragId(null);
            setDragOverId(null);
          }}
        >
          <button
            role="tab"
            aria-selected={w.assignmentId === activeId}
            className={`tab-bar__tab${w.assignmentId === activeId ? ' tab-bar__tab--active' : ''}`}
            onClick={() => onSelect(w.assignmentId)}
          >
            {w.displayName ?? w.site}
          </button>
          <button
            type="button"
            className="tab-bar__menu-btn"
            ref={(el) => {
              if (el) kebabRefs.current.set(w.assignmentId, el);
              else kebabRefs.current.delete(w.assignmentId);
            }}
            aria-label={`Workspace actions for ${w.displayName ?? w.site}`}
            aria-haspopup="menu"
            aria-expanded={openMenuId === w.assignmentId}
            onClick={(e) => {
              e.stopPropagation();
              setOpenMenuId((prev) => (prev === w.assignmentId ? null : w.assignmentId));
            }}
          >
            &#8942;
          </button>
          {openMenuId === w.assignmentId && (
            <div className="tab-bar__menu" role="menu">
              <button
                type="button"
                role="menuitem"
                className="tab-bar__menu-item"
                onClick={() => {
                  setOpenMenuId(null);
                  onEdit(w, kebabRefs.current.get(w.assignmentId) ?? null);
                }}
              >
                Edit Workspace
              </button>
              <button
                type="button"
                role="menuitem"
                className="tab-bar__menu-item"
                disabled={index === 0}
                onClick={() => {
                  setOpenMenuId(null);
                  moveTab(w.assignmentId, -1);
                }}
              >
                Move Left
              </button>
              <button
                type="button"
                role="menuitem"
                className="tab-bar__menu-item"
                disabled={index === workspaces.length - 1}
                onClick={() => {
                  setOpenMenuId(null);
                  moveTab(w.assignmentId, 1);
                }}
              >
                Move Right
              </button>
              <button
                type="button"
                role="menuitem"
                className="tab-bar__menu-item"
                onClick={() => {
                  setOpenMenuId(null);
                  onArchive(w, kebabRefs.current.get(w.assignmentId) ?? null);
                }}
              >
                Archive Workspace
              </button>
              <button
                type="button"
                role="menuitem"
                className="tab-bar__menu-item tab-bar__menu-item--destructive"
                onClick={() => {
                  setOpenMenuId(null);
                  onDelete(w, kebabRefs.current.get(w.assignmentId) ?? null);
                }}
              >
                Delete Permanently
              </button>
            </div>
          )}
        </div>
      ))}

      <button
        className="tab-bar__add"
        onClick={(e) => onAdd(e.currentTarget)}
        title="Add workspace"
        aria-label="Add workspace"
      >
        +
      </button>

      <button
        className="tab-bar__manage"
        onClick={(e) => onManage(e.currentTarget)}
        title="Manage workspaces"
        aria-label="Manage workspaces"
      >
        &#9881;
      </button>

      <div className="tab-bar__spacer" aria-hidden="true" />

      <button
        role="tab"
        aria-selected={isGeneralActive}
        className={`tab-bar__tab tab-bar__tab--general${isGeneralActive ? ' tab-bar__tab--active' : ''}`}
        onClick={onSelectGeneral}
      >
        General
      </button>
    </div>
  );
}
