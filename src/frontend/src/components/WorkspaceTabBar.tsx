import { useLayoutEffect, useRef, useState } from 'react';
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
  isReportsActive?: boolean;
  onSelectReports?: () => void;
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
  isReportsActive = false,
  onSelectReports,
}: WorkspaceTabBarProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [dragOverId, setDragOverId] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const entryEdge = useRef<'first' | 'last'>('first');
  // Kebab (workspace-actions) trigger buttons are stable across a menu open/close cycle, so they
  // are the correct focus-restoration target for dialogs opened from a menu item (which unmounts
  // as soon as it's clicked).
  const kebabRefs = useRef<Map<string, HTMLButtonElement>>(new Map());

  useLayoutEffect(() => {
    if (!openMenuId) return;
    const trigger = kebabRefs.current.get(openMenuId);
    const items = menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitem"]:not(:disabled)');
    (entryEdge.current === 'last' ? items?.[items.length - 1] : items?.[0])?.focus({ preventScroll: true });
    const handlePointerDown = (e: PointerEvent) => {
      if (!menuRef.current?.contains(e.target as Node) && !trigger?.contains(e.target as Node)) {
        setOpenMenuId(null);
      }
    };
    const handleKeyDown = (e: KeyboardEvent) => {
      // Blocking dialogs and the existing analytical menu retain their own keyboard ownership.
      if (e.defaultPrevented || document.querySelector('[aria-modal="true"][role="dialog"], [aria-modal="true"][role="alertdialog"], [data-analytical-export-menu]')) return;
      if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        setOpenMenuId(null);
        trigger?.focus({ preventScroll: true });
      } else if (e.key === 'Tab') {
        // Anchor native forward/backward Tab progression at the stable trigger, not a removed item.
        setOpenMenuId(null);
        trigger?.focus({ preventScroll: true });
      }
    };
    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown, true);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown, true);
    };
  }, [openMenuId]);

  const moveTab = (assignmentId: string, delta: number) => {
    const index = workspaces.findIndex((w) => w.assignmentId === assignmentId);
    const targetIndex = index + delta;
    if (index === -1 || targetIndex < 0 || targetIndex >= workspaces.length) return;

    const reordered = [...workspaces];
    const [moved] = reordered.splice(index, 1);
    reordered.splice(targetIndex, 0, moved);
    kebabRefs.current.get(assignmentId)?.focus({ preventScroll: true });
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
    <div className="tab-bar" role="tablist" aria-label="Workspaces">
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
            aria-controls={openMenuId === w.assignmentId ? `workspace-actions-${w.assignmentId}` : undefined}
            onKeyDown={(e) => {
              if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;
              e.preventDefault();
              entryEdge.current = e.key === 'ArrowUp' ? 'last' : 'first';
              setOpenMenuId(w.assignmentId);
            }}
            onClick={(e) => {
              e.stopPropagation();
              entryEdge.current = 'first';
              setOpenMenuId((prev) => (prev === w.assignmentId ? null : w.assignmentId));
            }}
          >
            &#8942;
          </button>
          {openMenuId === w.assignmentId && (
            <div ref={menuRef} id={`workspace-actions-${w.assignmentId}`} className="tab-bar__menu" role="menu" aria-label={`Actions for ${w.displayName ?? w.site}`} data-workspace-action-menu
              onKeyDown={(e) => {
                if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key)) return;
                e.preventDefault();
                const items = Array.from(e.currentTarget.querySelectorAll<HTMLButtonElement>('[role="menuitem"]:not(:disabled)'));
                const index = items.indexOf(document.activeElement as HTMLButtonElement);
                items[e.key === 'Home' ? 0 : e.key === 'End' ? items.length - 1 : (index + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length]?.focus({ preventScroll: true });
              }}>
              <button
                type="button"
                role="menuitem"
                tabIndex={-1}
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
                tabIndex={-1}
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
                tabIndex={-1}
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
                tabIndex={-1}
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
                tabIndex={-1}
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
      {onSelectReports && <button role="tab" aria-selected={isReportsActive}
        className={`tab-bar__tab${isReportsActive ? ' tab-bar__tab--active' : ''}`}
        onClick={onSelectReports}>Reports</button>}
    </div>
  );
}
