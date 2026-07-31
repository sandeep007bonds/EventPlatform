import { Button, Form, Input, Space } from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';

/**
 * Repeatable (platform, URL) list editor for an open-ended set of social links — no fixed platform
 * list, so adding a new platform never needs a schema change. Must render inside an Ant `<Form>`;
 * `name` is the `Form.List` field name to bind to (defaults to `socialLinks`).
 */
export function SocialLinksEditor({ name = 'socialLinks' }: { name?: string }) {
  return (
    <Form.List name={name}>
      {(fields, { add, remove }) => (
        <>
          {fields.map((field) => (
            <Space key={field.key} align="baseline" wrap>
              <Form.Item
                name={[field.name, 'platform']}
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="Platform (e.g. Instagram)" />
              </Form.Item>
              <Form.Item
                name={[field.name, 'url']}
                rules={[
                  { required: true, message: 'Required' },
                  { type: 'url', message: 'Must be a URL' },
                ]}
              >
                <Input placeholder="https://..." style={{ width: 280 }} />
              </Form.Item>
              <MinusCircleOutlined onClick={() => remove(field.name)} />
            </Space>
          ))}
          <Form.Item>
            <Button
              type="dashed"
              onClick={() => add({ platform: '', url: '' })}
              icon={<PlusOutlined />}
            >
              Add social link
            </Button>
          </Form.Item>
        </>
      )}
    </Form.List>
  );
}
