import {
  CheckSquare,
  Code,
  Heading1,
  Heading2,
  Heading3,
  List,
  ListOrdered,
  Text,
  TextQuote,
  MinusSquare,
  TableIcon,
} from 'lucide-react';
import { Extension } from '@tiptap/core';
import Suggestion from '@tiptap/suggestion';
import { ReactRenderer } from '@tiptap/react';
import tippy from 'tippy.js';
import { CommandList } from './command-list';

export interface SuggestionItem {
  title: string;
  description: string;
  icon: React.ReactNode;
  command: ({ editor, range }: any) => void;
}

export const suggestionItems: SuggestionItem[] = [
  {
    title: 'Text',
    description: 'Just start typing with plain text.',
    icon: <Text size={18} />,
    command: ({ editor, range }) => {
      editor
        .chain()
        .focus()
        .deleteRange(range)
        .toggleNode('paragraph', 'paragraph')
        .run();
    },
  },
  {
    title: 'Heading 1',
    description: 'Big section heading.',
    icon: <Heading1 size={18} />,
    command: ({ editor, range }) => {
      editor
        .chain()
        .focus()
        .deleteRange(range)
        .setNode('heading', { level: 1 })
        .run();
    },
  },
  {
    title: 'Heading 2',
    description: 'Medium section heading.',
    icon: <Heading2 size={18} />,
    command: ({ editor, range }) => {
      editor
        .chain()
        .focus()
        .deleteRange(range)
        .setNode('heading', { level: 2 })
        .run();
    },
  },
  {
    title: 'Heading 3',
    description: 'Small section heading.',
    icon: <Heading3 size={18} />,
    command: ({ editor, range }) => {
      editor
        .chain()
        .focus()
        .deleteRange(range)
        .setNode('heading', { level: 3 })
        .run();
    },
  },
  {
    title: 'Bullet List',
    description: 'Create a simple bullet list.',
    icon: <List size={18} />,
    command: ({ editor, range }) => {
      editor.chain().focus().deleteRange(range).toggleBulletList().run();
    },
  },
  {
    title: 'Numbered List',
    description: 'Create a list with numbering.',
    icon: <ListOrdered size={18} />,
    command: ({ editor, range }) => {
      editor.chain().focus().deleteRange(range).toggleOrderedList().run();
    },
  },
  {
    title: 'Task List',
    description: 'Track tasks with a to-do list.',
    icon: <CheckSquare size={18} />,
    command: ({ editor, range }) => {
      editor.chain().focus().deleteRange(range).toggleTaskList().run();
    },
  },
  {
    title: 'Quote',
    description: 'Capture a quote.',
    icon: <TextQuote size={18} />,
    command: ({ editor, range }) =>
      editor
        .chain()
        .focus()
        .deleteRange(range)
        .toggleNode('paragraph', 'paragraph')
        .toggleBlockquote()
        .run(),
  },
  {
    title: 'Code',
    description: 'Capture a code snippet.',
    icon: <Code size={18} />,
    command: ({ editor, range }) =>
      editor.chain().focus().deleteRange(range).toggleCodeBlock().run(),
  },
  {
    title: 'Divider',
    description: 'Visually divide blocks.',
    icon: <MinusSquare size={18} />,
    command: ({ editor, range }) => {
      editor.chain().focus().deleteRange(range).setHorizontalRule().run();
    },
  },
  {
    title: 'Table',
    description: 'Insert a table.',
    icon: <TableIcon size={18} />,
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

export const slashCommand = Extension.create({
  name: 'slashCommand',

  addOptions() {
    return {
      suggestion: {
        char: '/',
        command: ({ editor, range, props }: any) => {
          props.command({ editor, range });
        },
      },
    };
  },

  addProseMirrorPlugins() {
    return [
      Suggestion({
        editor: this.editor,
        ...this.options.suggestion,
        items: ({ query }: any) => {
          return suggestionItems.filter((item) =>
            item.title.toLowerCase().includes(query.toLowerCase())
          );
        },
        render: () => {
          let component: ReactRenderer;
          let popup: any;

          return {
            onStart: (props: any) => {
              component = new ReactRenderer(CommandList, {
                props,
                editor: props.editor,
              });

              popup = tippy('body', {
                getReferenceClientRect: props.clientRect,
                appendTo: () => document.body,
                content: component.element,
                showOnCreate: true,
                interactive: true,
                trigger: 'manual',
                placement: 'bottom-start',
              });
            },

            onUpdate(props: any) {
              component.updateProps(props);

              popup[0].setProps({
                getReferenceClientRect: props.clientRect,
              });
            },

            onKeyDown(props: any) {
              if (props.event.key === 'Escape') {
                popup[0].hide();
                return true;
              }

              // @ts-expect-error - CommandList ref may not have onKeyDown method
              return component.ref?.onKeyDown(props);
            },

            onExit() {
              popup[0].destroy();
              component.destroy();
            },
          };
        },
      }),
    ];
  },
});
