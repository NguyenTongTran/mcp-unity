import * as z from 'zod';
import { Logger } from '../../utils/logger.js';
import { McpUnity } from '../../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'delete_asset';
const toolDescription = 'Deletes an asset (file or folder) from the project';
const paramsSchema = z.object({
  path: z.string().describe('The path of the asset to delete')
});

export function registerDeleteAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
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
  const { path } = params;
        
  if (!path) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameter "path" must be provided'
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      path
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to delete asset at ${path}`
    );
  }
  
  return {
    content: [{
      type: response.type,
      text: response.message
    }]
  };
}
