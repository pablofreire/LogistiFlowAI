import { Alert, Badge, Button, Card, Group, Stack, Text, Textarea, Title, Tooltip } from '@mantine/core';
import { IconAlertTriangle, IconMessageCircleQuestion, IconRefresh, IconSend, IconTool } from '@tabler/icons-react';
import { useState, type FormEvent } from 'react';
import { ApiError } from '../api/envelope';
import { useAskCopilot, useCopilotStatus } from '../api/hooks';
import type { CopilotAnswer, CopilotToolCall } from '../api/types';
import { formatNumber } from '../components/format';
import { LoadingState } from '../components/states/QueryState';

const maxQuestionLength = 500;

interface Turn {
  question: string;
  answer: CopilotAnswer;
}

export function CopilotPage() {
  const status = useCopilotStatus();
  const ask = useAskCopilot();
  const [question, setQuestion] = useState('');
  const [turns, setTurns] = useState<Turn[]>([]);
  const conversationId = turns.at(-1)?.answer.conversationId ?? null;

  if (status.isPending) {
    return <LoadingState label="Connecting to the copilot…" />;
  }

  if (status.error) {
    return (
      <Alert color="yellow" variant="light" icon={<IconAlertTriangle size={18} />} title="Copilot unavailable" data-testid="copilot-unavailable">
        The operations copilot is not responding. Orders, fulfillment and shipping keep working; try the copilot again later.
      </Alert>
    );
  }

  const submit = (event?: FormEvent) => {
    event?.preventDefault();
    const text = question.trim();
    if (text.length === 0 || ask.isPending) {
      return;
    }

    ask.mutate(
      { question: text, conversationId },
      {
        onSuccess: (answer) => {
          setTurns((previous) => [...previous, { question: text, answer }]);
          setQuestion('');
        },
      },
    );
  };

  const provider = status.data;
  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-end">
        <div>
          <Title order={1} fz={26}>
            Operations copilot
          </Title>
          <Text c="dimmed" size="sm">
            Answers from live platform data, read with your permissions. It suggests next steps and never changes anything.
          </Text>
        </div>
        <Tooltip label={provider.simulated ? 'Deterministic demo provider: no AI model is called' : `Model ${provider.model}`}>
          <Badge
            size="lg"
            variant="light"
            color={provider.simulated ? 'orange' : 'teal'}
            data-testid="copilot-provider"
            data-simulated={provider.simulated}
          >
            {provider.simulated ? 'Simulated · demo provider' : `${provider.provider} · ${provider.model}`}
          </Badge>
        </Tooltip>
      </Group>

      <Card>
        <form onSubmit={submit}>
          <Stack gap="sm">
            <Textarea
              label="Ask about orders, shipments, warehouses or exceptions"
              placeholder="Why is order ORD-1025 delayed?"
              value={question}
              onChange={(event) => setQuestion(event.currentTarget.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                  submit();
                  event.preventDefault();
                }
              }}
              maxLength={maxQuestionLength}
              autosize
              minRows={2}
              data-testid="copilot-question"
            />
            <Group justify="space-between">
              <Group gap={6}>
                {provider.sampleQuestions.map((sample) => (
                  <Button key={sample} size="compact-xs" variant="default" onClick={() => setQuestion(sample)} data-testid="copilot-sample">
                    {sample}
                  </Button>
                ))}
              </Group>
              <Group gap="xs">
                {turns.length > 0 && (
                  <Button variant="subtle" leftSection={<IconRefresh size={16} />} onClick={() => setTurns([])} data-testid="copilot-new-conversation">
                    New conversation
                  </Button>
                )}
                <Button type="submit" leftSection={<IconSend size={16} />} loading={ask.isPending} disabled={question.trim().length === 0} data-testid="copilot-ask">
                  Ask
                </Button>
              </Group>
            </Group>
          </Stack>
        </form>
      </Card>

      {ask.error && (
        <Alert color="red" variant="light" icon={<IconAlertTriangle size={18} />} title="No answer" data-testid="copilot-error">
          {ask.error instanceof ApiError ? ask.error.message : 'The copilot did not respond. Try again in a moment.'}
        </Alert>
      )}

      {turns.length === 0 ? (
        <Text c="dimmed" size="sm" data-testid="copilot-empty">
          Pick a sample question or write your own. Every answer lists the tools that produced it.
        </Text>
      ) : (
        <Stack gap="md">
          {[...turns].reverse().map((turn) => (
            <TurnCard key={turn.answer.interactionId} turn={turn} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}

function TurnCard({ turn }: { turn: Turn }) {
  const { answer } = turn;
  return (
    <Card data-testid="copilot-turn">
      <Group gap="xs" mb="xs" wrap="nowrap" align="flex-start">
        <IconMessageCircleQuestion size={18} style={{ flexShrink: 0, marginTop: 2 }} />
        <Text fw={600} data-testid="copilot-turn-question">
          {turn.question}
        </Text>
      </Group>
      <Text size="sm" style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }} data-testid="copilot-answer">
        {answer.answer}
      </Text>
      <Group gap={6} mt="md">
        <IconTool size={16} />
        {answer.toolCalls.length === 0 ? (
          <Text size="xs" c="dimmed">
            No tools were needed.
          </Text>
        ) : (
          answer.toolCalls.map((call) => <ToolCallBadge key={call.sequence} call={call} />)
        )}
      </Group>
      <Text size="xs" c="dimmed" mt={6} data-testid="copilot-usage">
        {answer.simulated ? 'Simulated (demo provider)' : `${answer.provider} · ${answer.model}`} · {formatNumber(answer.durationMs)} ms ·{' '}
        {answer.usage.totalTokens === null ? 'no tokens' : `${formatNumber(answer.usage.totalTokens)} tokens`} · ${answer.usage.estimatedCostUsd.toFixed(4)}
      </Text>
    </Card>
  );
}

function ToolCallBadge({ call }: { call: CopilotToolCall }) {
  const ok = call.outcome === 'ok';
  return (
    <Tooltip label={`${call.arguments} → ${call.summary} (${call.durationMs} ms)`} multiline maw={360}>
      <Badge variant="outline" color={ok ? 'blue' : 'red'} data-testid="copilot-tool-call" data-name={call.name} data-outcome={call.outcome}>
        {call.name}
        {ok ? '' : ` · ${call.outcome}`}
      </Badge>
    </Tooltip>
  );
}
