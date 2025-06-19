import * as z from 'zod';
import { Logger } from '../../utils/logger.js';
import { McpUnity } from '../../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'move_asset';
const toolDescription = 'Moves assets (files or folders) from source locations to a destination in the project';
const paramsSchema = z.object({
  sourcePaths: z.array(z.string()).describe('The source path(s) of the asset(s) to move'),
  destPath: z.string().describe('The destination path where the asset will be moved to')
});

export function registerMoveAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
  const { sourcePaths, destPath } = params;
        
  if (!sourcePaths || !destPath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameters "sourcePaths" and "destPath" must be provided'
    );
  }

  if (sourcePaths.length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'At least one source path must be provided'
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      sourcePaths,
      destPath
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to move assets to ${destPath}`
    );
  }

  return {
    content: [{
      type: response.type || 'text',
      text: response.message,
      errors: response.errors
    }]
  };
}
