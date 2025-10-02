import { Extension } from '@tiptap/core';
import { ReactRenderer } from '@tiptap/react';
import Suggestion, { type SuggestionOptions, type SuggestionProps } from '@tiptap/suggestion';
import { useEffect, useState, forwardRef, useImperativeHandle } from 'react';
import { cn } from '@/lib/utils';
import tippy, { type Instance as TippyInstance } from 'tippy.js';
import 'tippy.js/dist/tippy.css';

interface CommandItem {
  title: string;
  description: string;
  icon: string;
  command: (props: { editor: any; range: any }) => void;
}

interface CommandListProps {
  items: CommandItem[];
  command: (item: CommandItem) => void;
  editor: any;
  range: any;
}

export interface CommandListRef {
  onKeyDown: (props: { event: KeyboardEvent }) => boolean;
}

const CommandList = forwardRef<CommandListRef, CommandListProps>((props, ref) => {
  const [selectedIndex, setSelectedIndex] = useState(0);

  useEffect(() => {
    setSelectedIndex(0);
  }, [props.items]);

  useImperativeHandle(ref, () => ({
    onKeyDown: ({ event }: { event: KeyboardEvent }) => {
      if (event.key === 'ArrowUp') {
        event.preventDefault();
        setSelectedIndex((prev) => (prev + props.items.length - 1) % props.items.length);
        return true;
      }

      if (event.key === 'ArrowDown') {
        event.preventDefault();
        setSelectedIndex((prev) => (prev + 1) % props.items.length);
        return true;
      }

      if (event.key === 'Enter') {
        event.preventDefault();
        const item = props.items[selectedIndex];
        if (item) {
          item.command({ editor: props.editor, range: props.range });
        }
        return true;
      }

      return false;
    },
  }));

  if (props.items.length === 0) {
    return (
      <div className="z-50 min-w-[280px] rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-md p-2">
        <div className="text-sm text-gray-500 dark:text-gray-400 px-3 py-2">No results</div>
      </div>
    );
  }

  return (
    <div className="z-50 min-w-[280px] rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-md overflow-hidden">
      {props.items.map((item, index) => (
        <button
          key={index}
          type="button"
          className={cn(
            'flex w-full items-center gap-3 px-3 py-2 text-sm text-left transition-colors',
            'hover:bg-gray-100 dark:hover:bg-gray-700',
            index === selectedIndex && 'bg-gray-100 dark:bg-gray-700'
          )}
          onClick={() => item.command({ editor: props.editor, range: props.range })}
        >
          <span className="text-xl flex-shrink-0">{item.icon}</span>
          <div className="flex-1 min-w-0">
            <div className="font-medium text-gray-900 dark:text-gray-100">{item.title}</div>
            <div className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.description}</div>
          </div>
        </button>
      ))}
    </div>
  );
});

CommandList.displayName = 'CommandList';

const suggestionOptions: Omit<SuggestionOptions, 'editor'> = {
  items: ({ query }: { query: string }): CommandItem[] => {
    const items: CommandItem[] = [
      {
        title: 'Text',
        description: 'Just start typing with plain text.',
        icon: '📝',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).setParagraph().run();
        },
      },
      {
        title: 'Heading 1',
        description: 'Big section heading.',
        icon: '📌',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 1 }).run();
        },
      },
      {
        title: 'Heading 2',
        description: 'Medium section heading.',
        icon: '📍',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 2 }).run();
        },
      },
      {
        title: 'Heading 3',
        description: 'Small section heading.',
        icon: '📎',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).setNode('heading', { level: 3 }).run();
        },
      },
      {
        title: 'Bullet List',
        description: 'Create a simple bullet list.',
        icon: '•',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).toggleBulletList().run();
        },
      },
      {
        title: 'Numbered List',
        description: 'Create a list with numbering.',
        icon: '1️⃣',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).toggleOrderedList().run();
        },
      },
      {
        title: 'Task List',
        description: 'Track tasks with a to-do list.',
        icon: '☑️',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).toggleTaskList().run();
        },
      },
      {
        title: 'Quote',
        description: 'Capture a quote.',
        icon: '💬',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).toggleBlockquote().run();
        },
      },
      {
        title: 'Code Block',
        description: 'Display code with syntax highlighting.',
        icon: '💻',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).toggleCodeBlock().run();
        },
      },
      {
        title: 'Divider',
        description: 'Visually divide blocks.',
        icon: '➖',
        command: ({ editor, range }) => {
          editor.chain().focus().deleteRange(range).setHorizontalRule().run();
        },
      },
      {
        title: 'Table',
        description: 'Insert a table.',
        icon: '📊',
        command: ({ editor, range }) => {
          editor
            .chain()
            .focus()
            .deleteRange(range)
            .insertTable({ rows: 3, cols: 3, withHeaderRow: true })
            .run();
        },
      },
    ];

    return items.filter((item) =>
      item.title.toLowerCase().startsWith(query.toLowerCase())
    );
  },

  render: () => {
    let component: ReactRenderer<CommandListRef> | null = null;
    let popup: TippyInstance[] | null = null;

    return {
      onStart: (props: SuggestionProps) => {
        component = new ReactRenderer(CommandList, {
          props: {
            ...props,
            editor: props.editor,
            range: props.range,
          },
          editor: props.editor,
        });

        if (!props.clientRect) {
          return;
        }

        popup = tippy('body', {
          getReferenceClientRect: props.clientRect as () => DOMRect,
          appendTo: () => document.body,
          content: component.element,
          showOnCreate: true,
          interactive: true,
          trigger: 'manual',
          placement: 'bottom-start',
        });
      },

      onUpdate(props: SuggestionProps) {
        component?.updateProps({
          ...props,
          editor: props.editor,
          range: props.range,
        });

        if (!props.clientRect || !popup) {
          return;
        }

        popup[0]?.setProps({
          getReferenceClientRect: props.clientRect as () => DOMRect,
        });
      },

      onKeyDown(props: { event: KeyboardEvent }) {
        if (props.event.key === 'Escape') {
          popup?.[0]?.hide();
          return true;
        }

        return component?.ref?.onKeyDown(props) ?? false;
      },

      onExit() {
        popup?.[0]?.destroy();
        component?.destroy();
      },
    };
  },
};

export const SlashCommand = Extension.create({
  name: 'slashCommand',

  addOptions() {
    return {
      suggestion: {
        char: '/',
        startOfLine: false,
        command: ({ editor, range, props }: any) => {
          props.command({ editor, range });
        },
        ...suggestionOptions,
      },
    };
  },

  addProseMirrorPlugins() {
    return [
      Suggestion({
        editor: this.editor,
        ...this.options.suggestion,
      }),
    ];
  },
});
