import * as z from 'zod';
import { Logger } from '../../utils/logger.js';
import { McpUnity } from '../../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'pack_atlas';
const toolDescription = 'Packs sprite atlases - supports packing specific atlases or all atlases';
const paramsSchema = z.object({
  paths: z.union([
    z.literal('all'),
    z.array(z.string()).min(1)
  ]).describe('Either "all" to pack all atlases or an array of atlas paths')
});

export function registerPackAtlasTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
      
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { paths } = params;

  if (!paths) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameter "paths" must be provided as either a string "all" or an array of atlas paths'
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      paths
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.errors,
    );
  }

  return {
    content: [{
      type: response.type || 'text',
      text: response.message,
    }]
  };
}
