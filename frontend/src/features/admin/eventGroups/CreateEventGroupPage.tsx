import { useState } from 'react';
import { Button, Card, Form, Input, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import { createEventGroup } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';

interface CreateEventGroupFormValues {
  title: string;
}

/** Creates a new tour (event group) for the caller's tenant. */
export function CreateEventGroupPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (values: CreateEventGroupFormValues) => {
    setSubmitting(true);
    try {
      await createEventGroup({ title: values.title });
      toast.success('Tour created.');
      void navigate('/admin/tours');
    } catch {
      toast.error('Could not create the tour.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card style={{ maxWidth: 480 }}>
      <Typography.Title level={3}>Create tour</Typography.Title>
      <Form<CreateEventGroupFormValues>
        layout="vertical"
        onFinish={(values) => {
          void handleSubmit(values);
        }}
      >
        <Form.Item name="title" label="Title" rules={[{ required: true }]}>
          <Input placeholder="e.g. Coldplay World Tour" />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={submitting}>
            Create
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
}
