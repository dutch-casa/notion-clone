import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import type { SuggestionItem } from './slash-command-novel';

interface CommandListProps {
  items: SuggestionItem[];
  command: (item: SuggestionItem) => void;
}

export const CommandList = forwardRef((props: CommandListProps, ref) => {
  const [selectedIndex, setSelectedIndex] = useState(0);

  const selectItem = (index: number) => {
    const item = props.items[index];
    if (item) {
      props.command(item);
    }
  };

  const upHandler = () => {
    setSelectedIndex((selectedIndex + props.items.length - 1) % props.items.length);
  };

  const downHandler = () => {
    setSelectedIndex((selectedIndex + 1) % props.items.length);
  };

  const enterHandler = () => {
    selectItem(selectedIndex);
  };

  useEffect(() => setSelectedIndex(0), [props.items]);

  useImperativeHandle(ref, () => ({
    onKeyDown: ({ event }: { event: KeyboardEvent }) => {
      if (event.key === 'ArrowUp') {
        upHandler();
        return true;
      }

      if (event.key === 'ArrowDown') {
        downHandler();
        return true;
      }

      if (event.key === 'Enter') {
        enterHandler();
        return true;
      }

      return false;
    },
  }));

  return (
    <div className="z-50 min-w-[12rem] overflow-hidden rounded-md border bg-popover p-1 text-popover-foreground shadow-md">
      {props.items.length ? (
        props.items.map((item, index) => (
          <button
            className={`relative flex w-full cursor-pointer select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground ${
              index === selectedIndex ? 'bg-accent text-accent-foreground' : ''
            }`}
            key={index}
            onClick={() => selectItem(index)}
            type="button"
          >
            <div className="flex items-center gap-2">
              <div className="flex items-center justify-center">{item.icon}</div>
              <div className="flex flex-col items-start">
                <div className="font-medium">{item.title}</div>
                <div className="text-xs text-muted-foreground">{item.description}</div>
              </div>
            </div>
          </button>
        ))
      ) : (
        <div className="px-2 py-1.5 text-sm text-muted-foreground">No results</div>
      )}
    </div>
  );
});

CommandList.displayName = 'CommandList';
