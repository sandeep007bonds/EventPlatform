import { useMemo, useState } from 'react';
import { Button, Card, Input, Space, Table, Typography } from 'antd';
import { DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import type { TableProps } from 'antd';
import { toCsv, downloadCsv, timestampedFileName, type CsvColumn } from './csv';

/**
 * A grid column: everything Ant's own column accepts, plus how the cell exports.
 *
 * `exportValue` exists because `render` returns React nodes — a `<Tag>`, a formatted money string,
 * a link — and none of those belong in a CSV. Declaring the export separately keeps the file
 * readable without stringifying markup.
 */
export type DataGridColumn<T> = NonNullable<TableProps<T>['columns']>[number] & {
  /** The cell's exported value. Falls back to the raw `dataIndex` value when omitted. */
  exportValue?: (row: T) => string | number | boolean | null | undefined;
  /** Include this column when the search box filters rows. */
  searchable?: boolean;
};

export interface DataGridProps<T> extends Omit<TableProps<T>, 'columns' | 'dataSource'> {
  columns: DataGridColumn<T>[];
  rows: T[];
  /** Placeholder for the search box. Omit to hide it. */
  searchPlaceholder?: string;
  /** Base name for the exported file, without extension. Omit to hide the export button. */
  exportFileName?: string;
  /** Extra toolbar content, rendered to the right of search. */
  toolbarExtra?: React.ReactNode;
  /**
   * Shown at the right of the toolbar — e.g. "1,204 orders".
   *
   * Not called `summary`: Ant's `Table` already has one, and it means the footer summary row.
   */
  countLabel?: React.ReactNode;
}

/**
 * The admin tables' shared grid: Ant's `Table` plus search, sorting, filtering and CSV export.
 *
 * Sorting and column filters are Ant's own — this adds nothing to them. What it does add is a
 * single free-text search across the columns marked `searchable`, and an export of exactly what
 * the reader is looking at.
 *
 * **The export covers the rows this grid was given, which is not always all of them.** Where the
 * page loads server-side, `rows` is one page, so this button exports one page. That is the honest
 * behaviour for a client-side export and the reason a server-side export exists separately: it is
 * the API's job to produce a file covering everything matching a query, not the browser's.
 */
export function DataGrid<T extends object>({
  columns,
  rows,
  searchPlaceholder,
  exportFileName,
  toolbarExtra,
  countLabel,
  ...tableProps
}: DataGridProps<T>) {
  const [search, setSearch] = useState('');

  const searchable = useMemo(() => columns.filter((column) => column.searchable), [columns]);

  const visibleRows = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term || searchable.length === 0) {
      return rows;
    }
    return rows.filter((row) =>
      searchable.some((column) => String(cellValue(column, row) ?? '').toLowerCase().includes(term)),
    );
  }, [rows, search, searchable]);

  const handleExport = () => {
    if (!exportFileName) {
      return;
    }
    const csvColumns: CsvColumn<T>[] = columns.map((column) => ({
      header: typeof column.title === 'string' ? column.title : String(column.key ?? ''),
      value: (row) => cellValue(column, row),
    }));
    downloadCsv(timestampedFileName(exportFileName), toCsv(visibleRows, csvColumns));
  };

  const showToolbar = Boolean(searchPlaceholder || exportFileName || toolbarExtra || countLabel);

  return (
    <Card styles={{ body: { padding: 16 } }}>
      {showToolbar && (
        <Space style={{ marginBottom: 12, width: '100%', justifyContent: 'space-between' }} wrap>
          <Space wrap>
            {searchPlaceholder && (
              <Input
                allowClear
                prefix={<SearchOutlined />}
                placeholder={searchPlaceholder}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                style={{ width: 260 }}
              />
            )}
            {toolbarExtra}
          </Space>
          <Space wrap>
            {countLabel && <Typography.Text type="secondary">{countLabel}</Typography.Text>}
            {exportFileName && (
              <Button
                icon={<DownloadOutlined />}
                onClick={handleExport}
                disabled={visibleRows.length === 0}
              >
                Export CSV
              </Button>
            )}
          </Space>
        </Space>
      )}
      <Table<T> {...tableProps} columns={columns} dataSource={visibleRows} />
    </Card>
  );
}

/** The exportable/searchable value of one cell: the column's own accessor, else its raw field. */
function cellValue<T>(
  column: DataGridColumn<T>,
  row: T,
): string | number | boolean | null | undefined {
  if (column.exportValue) {
    return column.exportValue(row);
  }
  const dataIndex = 'dataIndex' in column ? column.dataIndex : undefined;
  if (typeof dataIndex !== 'string') {
    return undefined;
  }
  const value = (row as Record<string, unknown>)[dataIndex];
  return typeof value === 'string' ||
    typeof value === 'number' ||
    typeof value === 'boolean' ||
    value === null ||
    value === undefined
    ? value
    : String(value);
}
